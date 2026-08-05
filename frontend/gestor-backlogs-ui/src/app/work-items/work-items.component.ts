import { Component, OnInit, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../services/api.service';
import {
  GenerateTasksItemResult,
  GenerateTasksResult,
  Iteration,
  RegenerateTasksResult,
  WorkItemPreview,
  WorkItemTask,
} from '../models/api-models';

@Component({
  selector: 'app-work-items',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './work-items.component.html',
  styles: ':host { display: contents; }',
})
export class WorkItemsComponent implements OnInit {
  sprints = signal<Iteration[]>([]);
  selectedIterationPath = signal<string>('');
  workItems = signal<WorkItemPreview[]>([]);
  selectedTypes = signal<Set<string>>(new Set());
  selectedIds = signal<Set<number>>(new Set());
  loadingSprints = signal(false);
  loadingWorkItems = signal(false);
  hasLoadedOnce = signal(false);
  areaPathConfigured = signal<boolean | null>(null);
  generating = signal(false);
  errorMessage = signal('');
  result = signal<GenerateTasksResult | null>(null);

  orgUrl = signal('');
  project = signal('');

  expandedIds = signal<Set<number>>(new Set());
  loadingTasksFor = signal<Set<number>>(new Set());
  childTasksByParent = signal<Map<number, WorkItemTask[]>>(new Map());

  regeneratingIds = signal<Set<number>>(new Set());
  regenerateResults = signal<Map<number, RegenerateTasksResult>>(new Map());
  regenerateErrors = signal<Map<number, string>>(new Map());

  availableTypes = computed(() => [...new Set(this.workItems().map((wi) => wi.workItemType))].sort());

  eligibleItems = computed(() => this.workItems().filter((wi) => this.selectedTypes().has(wi.workItemType)));

  selectableItems = computed(() => this.eligibleItems().filter((wi) => !wi.alreadyHasTasks && wi.sizeRecognized));

  selectableCount = computed(() => this.selectableItems().length);

  allSelected = computed(() => {
    const selectable = this.selectableItems();
    return selectable.length > 0 && selectable.every((wi) => this.selectedIds().has(wi.id));
  });

  constructor(private readonly api: ApiService) {}

  ngOnInit(): void {
    this.loadSprints();

    this.api.getConnectionSettings().subscribe({
      next: (settings) => {
        this.areaPathConfigured.set(!!settings.areaPath?.trim());
        this.orgUrl.set(settings.organizationUrl);
        this.project.set(settings.project);
      },
      error: () => this.areaPathConfigured.set(false),
    });
  }

  workItemUrl(id: number): string {
    return `${this.orgUrl()}/${this.project()}/_workitems/edit/${id}`;
  }

  loadSprints(): void {
    this.loadingSprints.set(true);
    this.errorMessage.set('');

    this.api.getSprints().subscribe({
      next: (sprints) => {
        this.sprints.set(sprints);
        const current = sprints.find((s) => s.isCurrent) ?? sprints[0];
        if (current) {
          this.selectedIterationPath.set(current.path);
        }
        this.loadingSprints.set(false);
      },
      error: (err) => {
        this.loadingSprints.set(false);
        this.errorMessage.set(err?.error?.message ?? 'Erro ao carregar sprints.');
      },
    });
  }

  onSprintChange(path: string): void {
    this.selectedIterationPath.set(path);
    this.result.set(null);
    this.workItems.set([]);
    this.hasLoadedOnce.set(false);
  }

  loadSelectedSprint(): void {
    if (!this.selectedIterationPath()) {
      return;
    }
    this.hasLoadedOnce.set(true);
    this.loadWorkItems(this.selectedIterationPath());
  }

  loadWorkItems(iterationPath: string): void {
    this.loadingWorkItems.set(true);
    this.errorMessage.set('');

    this.api.getWorkItems(iterationPath).subscribe({
      next: (items) => {
        this.workItems.set(items);
        this.selectedTypes.set(new Set(items.map((i) => i.workItemType)));
        this.selectedIds.set(
          new Set(items.filter((i) => !i.alreadyHasTasks && i.sizeRecognized).map((i) => i.id)),
        );
        this.loadingWorkItems.set(false);
      },
      error: (err) => {
        this.loadingWorkItems.set(false);
        this.errorMessage.set(err?.error?.message ?? 'Erro ao carregar work items da sprint.');
      },
    });
  }

  toggleType(type: string, checked: boolean): void {
    const types = new Set(this.selectedTypes());
    checked ? types.add(type) : types.delete(type);
    this.selectedTypes.set(types);
  }

  toggleItem(id: number, checked: boolean): void {
    const ids = new Set(this.selectedIds());
    checked ? ids.add(id) : ids.delete(id);
    this.selectedIds.set(ids);
  }

  toggleAll(checked: boolean): void {
    const ids = new Set(this.selectedIds());
    for (const item of this.selectableItems()) {
      checked ? ids.add(item.id) : ids.delete(item.id);
    }
    this.selectedIds.set(ids);
  }

  toggleExpand(id: number): void {
    const expanded = new Set(this.expandedIds());
    if (expanded.has(id)) {
      expanded.delete(id);
      this.expandedIds.set(expanded);
      return;
    }

    expanded.add(id);
    this.expandedIds.set(expanded);

    if (!this.childTasksByParent().has(id)) {
      this.loadChildTasks(id);
    }
  }

  private loadChildTasks(id: number): void {
    const loading = new Set(this.loadingTasksFor());
    loading.add(id);
    this.loadingTasksFor.set(loading);

    this.api.getChildTasks(id).subscribe({
      next: (tasks) => this.storeChildTasks(id, tasks),
      error: () => this.storeChildTasks(id, []),
    });
  }

  private storeChildTasks(id: number, tasks: WorkItemTask[]): void {
    const cache = new Map(this.childTasksByParent());
    cache.set(id, tasks);
    this.childTasksByParent.set(cache);

    const loading = new Set(this.loadingTasksFor());
    loading.delete(id);
    this.loadingTasksFor.set(loading);
  }

  regenerate(item: WorkItemPreview): void {
    const confirmed = window.confirm(
      `Isso vai fechar a(s) task(s) existente(s) de "${item.title}" e criar novas seguindo o padrão de tamanho (${item.sizeLabel ?? item.effortHours + 'h'}). Continuar?`,
    );
    if (!confirmed) {
      return;
    }

    const active = new Set(this.regeneratingIds());
    active.add(item.id);
    this.regeneratingIds.set(active);

    const errors = new Map(this.regenerateErrors());
    errors.delete(item.id);
    this.regenerateErrors.set(errors);

    this.api.regenerateTasks(item.id).subscribe({
      next: (result) => {
        this.finishRegenerating(item.id);

        const results = new Map(this.regenerateResults());
        results.set(item.id, result);
        this.regenerateResults.set(results);

        const expanded = new Set(this.expandedIds());
        expanded.add(item.id);
        this.expandedIds.set(expanded);
        this.loadChildTasks(item.id);
      },
      error: (err) => {
        this.finishRegenerating(item.id);

        const errors = new Map(this.regenerateErrors());
        errors.set(item.id, err?.error?.message ?? 'Erro ao fechar/gerar tasks.');
        this.regenerateErrors.set(errors);
      },
    });
  }

  private finishRegenerating(id: number): void {
    const active = new Set(this.regeneratingIds());
    active.delete(id);
    this.regeneratingIds.set(active);
  }

  formatCreatedTasks(item: GenerateTasksItemResult): string {
    return item.createdTasks.map((t) => `${t.title} (${t.hoursEstimate}h)`).join(', ');
  }

  generate(): void {
    const ids = [...this.selectedIds()];
    if (ids.length === 0) {
      return;
    }

    this.generating.set(true);
    this.errorMessage.set('');

    this.api.generateTasks({ iterationPath: this.selectedIterationPath(), workItemIds: ids }).subscribe({
      next: (result) => {
        this.result.set(result);
        this.generating.set(false);
        this.loadWorkItems(this.selectedIterationPath());
      },
      error: (err) => {
        this.generating.set(false);
        this.errorMessage.set(err?.error?.message ?? 'Erro ao gerar tasks.');
      },
    });
  }
}

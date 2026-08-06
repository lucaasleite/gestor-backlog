import { Component, OnInit, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../services/api.service';
import { FilterDropdownComponent } from '../shared/filter-dropdown/filter-dropdown.component';
import {
  GenerateTasksItemResult,
  GenerateTasksResult,
  Iteration,
  PlannedTask,
  RegenerateTasksResult,
  TaskOverride,
  TeamConfig,
  WorkItemPreview,
  WorkItemTask,
} from '../models/api-models';

@Component({
  selector: 'app-work-items',
  standalone: true,
  imports: [FormsModule, FilterDropdownComponent],
  templateUrl: './work-items.component.html',
  styles: ':host { display: contents; }',
})
export class WorkItemsComponent implements OnInit {
  teams = signal<TeamConfig[]>([]);
  selectedTeamName = signal<string>('');

  sprints = signal<Iteration[]>([]);
  selectedIterationPath = signal<string>('');
  workItems = signal<WorkItemPreview[]>([]);
  selectedTypes = signal<Set<string>>(new Set());
  selectedAssignees = signal<Set<string>>(new Set());
  selectedStates = signal<Set<string>>(new Set());
  selectedIds = signal<Set<number>>(new Set());
  loadingSprints = signal(false);
  loadingWorkItems = signal(false);
  hasLoadedOnce = signal(false);
  generating = signal(false);
  errorMessage = signal('');
  result = signal<GenerateTasksResult | null>(null);

  orgUrl = signal('');
  project = signal('');

  expandedIds = signal<Set<number>>(new Set());
  loadingTasksFor = signal<Set<number>>(new Set());
  childTasksByParent = signal<Map<number, WorkItemTask[]>>(new Map());

  // Horas editáveis da prévia de cada item (clonadas de plannedTasks ao carregar; o usuário pode ajustar
  // antes de gerar - útil pra casos onde a task real é bem menor que o tamanho padrão da US).
  taskOverridesByItem = signal<Map<number, PlannedTask[]>>(new Map());

  regeneratingIds = signal<Set<number>>(new Set());
  regenerateResults = signal<Map<number, RegenerateTasksResult>>(new Map());
  regenerateErrors = signal<Map<number, string>>(new Map());

  selectedTeam = computed(() => this.teams().find((t) => t.name === this.selectedTeamName()) ?? null);

  availableTypes = computed(() => [...new Set(this.workItems().map((wi) => wi.workItemType))].sort());
  availableAssignees = computed(() =>
    [...new Set(this.workItems().map((wi) => wi.assignedTo ?? '(Sem responsável)'))].sort(),
  );
  availableStates = computed(() => [...new Set(this.workItems().map((wi) => wi.state ?? '(Sem status)'))].sort());

  // Segue a semântica do Azure DevOps: nenhuma opção marcada num filtro = sem filtro (mostra tudo).
  eligibleItems = computed(() => {
    const types = this.selectedTypes();
    const assignees = this.selectedAssignees();
    const states = this.selectedStates();

    return this.workItems().filter(
      (wi) =>
        (types.size === 0 || types.has(wi.workItemType)) &&
        (assignees.size === 0 || assignees.has(wi.assignedTo ?? '(Sem responsável)')) &&
        (states.size === 0 || states.has(wi.state ?? '(Sem status)')),
    );
  });

  itemsWithoutTasks = computed(() => this.eligibleItems().filter((wi) => !wi.alreadyHasTasks));
  itemsWithTasks = computed(() => this.eligibleItems().filter((wi) => wi.alreadyHasTasks));

  selectableItems = computed(() => this.itemsWithoutTasks().filter((wi) => wi.sizeRecognized));

  selectableCount = computed(() => this.selectableItems().length);

  allSelected = computed(() => {
    const selectable = this.selectableItems();
    return selectable.length > 0 && selectable.every((wi) => this.selectedIds().has(wi.id));
  });

  constructor(private readonly api: ApiService) {}

  ngOnInit(): void {
    this.api.getConnectionSettings().subscribe({
      next: (settings) => {
        this.orgUrl.set(settings.organizationUrl);
        this.project.set(settings.project);
        this.teams.set(settings.teams);
        if (settings.teams.length > 0) {
          this.selectedTeamName.set(settings.teams[0].name);
          this.loadSprints();
        }
      },
      error: () => {
        // Sem config salva, o guard já teria redirecionado pra /config antes de chegar aqui.
      },
    });
  }

  workItemUrl(id: number): string {
    return `${this.orgUrl()}/${this.project()}/_workitems/edit/${id}`;
  }

  onTeamChange(name: string): void {
    this.selectedTeamName.set(name);
    this.sprints.set([]);
    this.selectedIterationPath.set('');
    this.workItems.set([]);
    this.hasLoadedOnce.set(false);
    this.result.set(null);
    this.loadSprints();
  }

  loadSprints(): void {
    const team = this.selectedTeam();
    if (!team) {
      return;
    }

    this.loadingSprints.set(true);
    this.errorMessage.set('');

    this.api.getSprints(team.name).subscribe({
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

    const areaPath = this.selectedTeam()?.areaPath || null;

    this.api.getWorkItems(iterationPath, areaPath).subscribe({
      next: (items) => {
        this.workItems.set(items);
        this.selectedTypes.set(new Set());
        this.selectedAssignees.set(new Set());
        this.selectedStates.set(new Set());
        this.selectedIds.set(
          new Set(items.filter((i) => !i.alreadyHasTasks && i.sizeRecognized).map((i) => i.id)),
        );

        const overrides = new Map<number, PlannedTask[]>();
        for (const item of items) {
          overrides.set(item.id, item.plannedTasks.map((t) => ({ ...t })));
        }
        this.taskOverridesByItem.set(overrides);

        this.loadingWorkItems.set(false);
      },
      error: (err) => {
        this.loadingWorkItems.set(false);
        this.errorMessage.set(err?.error?.message ?? 'Erro ao carregar work items da sprint.');
      },
    });
  }

  plannedTasksFor(itemId: number): PlannedTask[] {
    return this.taskOverridesByItem().get(itemId) ?? [];
  }

  updateTaskHours(itemId: number, index: number, hours: number): void {
    const overrides = new Map(this.taskOverridesByItem());
    const tasks = [...(overrides.get(itemId) ?? [])];
    if (tasks[index]) {
      tasks[index] = { ...tasks[index], hours };
      overrides.set(itemId, tasks);
      this.taskOverridesByItem.set(overrides);
    }
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

  toggleExpand(item: WorkItemPreview): void {
    const expanded = new Set(this.expandedIds());
    if (expanded.has(item.id)) {
      expanded.delete(item.id);
      this.expandedIds.set(expanded);
      return;
    }

    expanded.add(item.id);
    this.expandedIds.set(expanded);

    if (item.alreadyHasTasks && !this.childTasksByParent().has(item.id)) {
      this.loadChildTasks(item.id);
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

    const overrides: Record<number, TaskOverride[]> = {};
    for (const id of ids) {
      overrides[id] = this.plannedTasksFor(id);
    }

    this.api.generateTasks({ iterationPath: this.selectedIterationPath(), workItemIds: ids, overrides }).subscribe({
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

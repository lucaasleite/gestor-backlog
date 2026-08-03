import { Component, OnInit, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatTableModule } from '@angular/material/table';
import { MatChipsModule } from '@angular/material/chips';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';
import { ApiService } from '../services/api.service';
import { GenerateTasksItemResult, GenerateTasksResult, Iteration, WorkItemPreview } from '../models/api-models';

@Component({
  selector: 'app-work-items',
  standalone: true,
  imports: [
    FormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatSelectModule,
    MatCheckboxModule,
    MatTableModule,
    MatChipsModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    MatIconModule,
  ],
  templateUrl: './work-items.component.html',
  styleUrl: './work-items.component.css',
})
export class WorkItemsComponent implements OnInit {
  displayedColumns = ['select', 'title', 'workItemType', 'size', 'effort', 'assignedTo', 'status'];

  sprints = signal<Iteration[]>([]);
  selectedIterationPath = signal<string>('');
  workItems = signal<WorkItemPreview[]>([]);
  selectedTypes = signal<Set<string>>(new Set());
  selectedIds = signal<Set<number>>(new Set());
  loadingSprints = signal(false);
  loadingWorkItems = signal(false);
  generating = signal(false);
  errorMessage = signal('');
  result = signal<GenerateTasksResult | null>(null);

  availableTypes = computed(() => [...new Set(this.workItems().map((wi) => wi.workItemType))].sort());

  eligibleItems = computed(() => this.workItems().filter((wi) => this.selectedTypes().has(wi.workItemType)));

  selectableCount = computed(
    () => this.eligibleItems().filter((wi) => !wi.alreadyHasTasks && wi.sizeRecognized).length,
  );

  constructor(private readonly api: ApiService) {}

  ngOnInit(): void {
    this.loadSprints();
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
          this.loadWorkItems(current.path);
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
    this.loadWorkItems(path);
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

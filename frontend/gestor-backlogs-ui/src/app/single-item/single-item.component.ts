import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../services/api.service';
import {
  GenerateTasksItemResult,
  PlannedTask,
  RegenerateTasksResult,
  WorkItemPreview,
  WorkItemTask,
} from '../models/api-models';

@Component({
  selector: 'app-single-item',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './single-item.component.html',
  styles: ':host { display: contents; }',
})
export class SingleItemComponent implements OnInit {
  workItemIdInput = signal('');
  loading = signal(false);
  error = signal('');
  item = signal<WorkItemPreview | null>(null);

  orgUrl = signal('');
  project = signal('');

  loadingChildTasks = signal(false);
  childTasks = signal<WorkItemTask[]>([]);
  taskOverrides = signal<PlannedTask[]>([]);

  closingTaskIds = signal<Set<number>>(new Set());
  closeTaskErrors = signal<Map<number, string>>(new Map());

  generating = signal(false);
  generateError = signal('');
  generateResult = signal<GenerateTasksItemResult | null>(null);

  regenerating = signal(false);
  regenerateError = signal('');
  regenerateResult = signal<RegenerateTasksResult | null>(null);

  constructor(private readonly api: ApiService) {}

  ngOnInit(): void {
    this.api.getConnectionSettings().subscribe({
      next: (settings) => {
        this.orgUrl.set(settings.organizationUrl);
        this.project.set(settings.project);
      },
      error: () => {
        // Sem config salva, os links de work item ficam vazios - o restante da tela ainda funciona.
      },
    });
  }

  workItemUrl(id: number): string {
    return `${this.orgUrl()}/${this.project()}/_workitems/edit/${id}`;
  }

  search(): void {
    const id = Number(this.workItemIdInput());
    if (!id) {
      this.error.set('Informe um ID de work item válido.');
      return;
    }

    this.loading.set(true);
    this.error.set('');
    this.item.set(null);
    this.childTasks.set([]);
    this.taskOverrides.set([]);
    this.closeTaskErrors.set(new Map());
    this.generateResult.set(null);
    this.generateError.set('');
    this.regenerateResult.set(null);
    this.regenerateError.set('');

    this.api.getWorkItemPreview(id).subscribe({
      next: (item) => {
        this.item.set(item);
        this.taskOverrides.set(item.plannedTasks.map((t) => ({ ...t })));
        this.loading.set(false);

        if (item.alreadyHasTasks) {
          this.loadChildTasks();
        }
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err?.error?.message ?? 'Erro ao buscar o work item.');
      },
    });
  }

  updateTaskHours(index: number, hours: number): void {
    const tasks = [...this.taskOverrides()];
    if (tasks[index]) {
      tasks[index] = { ...tasks[index], hours };
      this.taskOverrides.set(tasks);
    }
  }

  private loadChildTasks(): void {
    const item = this.item();
    if (!item) {
      return;
    }

    this.loadingChildTasks.set(true);
    this.api.getChildTasks(item.id).subscribe({
      next: (tasks) => {
        this.childTasks.set(tasks);
        this.loadingChildTasks.set(false);
      },
      error: () => {
        this.childTasks.set([]);
        this.loadingChildTasks.set(false);
      },
    });
  }

  closeTask(task: WorkItemTask): void {
    const confirmed = window.confirm(`Isso vai fechar a task "${task.title}" (#${task.id}). Continuar?`);
    if (!confirmed) {
      return;
    }

    const closing = new Set(this.closingTaskIds());
    closing.add(task.id);
    this.closingTaskIds.set(closing);

    const errors = new Map(this.closeTaskErrors());
    errors.delete(task.id);
    this.closeTaskErrors.set(errors);

    this.api.closeWorkItem(task.id).subscribe({
      next: () => {
        this.finishClosingTask(task.id);
        this.loadChildTasks();
      },
      error: (err) => {
        this.finishClosingTask(task.id);
        const errs = new Map(this.closeTaskErrors());
        errs.set(task.id, err?.error?.message ?? 'Erro ao fechar a task.');
        this.closeTaskErrors.set(errs);
      },
    });
  }

  private finishClosingTask(id: number): void {
    const closing = new Set(this.closingTaskIds());
    closing.delete(id);
    this.closingTaskIds.set(closing);
  }

  generate(): void {
    const item = this.item();
    if (!item) {
      return;
    }

    this.generating.set(true);
    this.generateError.set('');

    this.api.generateTaskForItem(item.id, this.taskOverrides()).subscribe({
      next: (result) => {
        this.generating.set(false);
        this.generateResult.set(result);
        this.refreshAfterChange(item.id);
      },
      error: (err) => {
        this.generating.set(false);
        this.generateError.set(err?.error?.message ?? 'Erro ao gerar tasks.');
      },
    });
  }

  regenerate(): void {
    const item = this.item();
    if (!item) {
      return;
    }

    const confirmed = window.confirm(
      `Isso vai fechar a(s) task(s) existente(s) de "${item.title}" e criar novas seguindo o padrão de tamanho (${item.sizeLabel ?? item.effortHours + 'h'}). Continuar?`,
    );
    if (!confirmed) {
      return;
    }

    this.regenerating.set(true);
    this.regenerateError.set('');

    this.api.regenerateTasks(item.id).subscribe({
      next: (result) => {
        this.regenerating.set(false);
        this.regenerateResult.set(result);
        this.refreshAfterChange(item.id);
      },
      error: (err) => {
        this.regenerating.set(false);
        this.regenerateError.set(err?.error?.message ?? 'Erro ao fechar/gerar tasks.');
      },
    });
  }

  private refreshAfterChange(id: number): void {
    this.api.getWorkItemPreview(id).subscribe({
      next: (item) => {
        this.item.set(item);
        this.taskOverrides.set(item.plannedTasks.map((t) => ({ ...t })));
      },
    });

    this.loadChildTasks();
  }
}

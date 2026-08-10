import { Component, OnInit, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../services/api.service';
import { GenerateTasksResult, Iteration, ParentUserStory, TeamConfig, WorkItemTask } from '../models/api-models';

interface PreviewItem {
  title: string;
  assignedTo: string | null;
}

@Component({
  selector: 'app-parent-tasks',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './parent-tasks.component.html',
  styles: ':host { display: contents; }',
})
export class ParentTasksComponent implements OnInit {
  parentId = signal('');
  loadingParent = signal(false);
  parentError = signal('');
  userStories = signal<ParentUserStory[]>([]);
  selectedUsIds = signal<Set<number>>(new Set());

  teams = signal<TeamConfig[]>([]);
  selectedTeamName = signal('');

  sprints = signal<Iteration[]>([]);
  selectedSprintPaths = signal<Set<string>>(new Set());

  closingTaskIds = signal<Set<number>>(new Set());
  closeTaskErrors = signal<Map<number, string>>(new Map());

  generating = signal(false);
  errorMessage = signal('');
  result = signal<GenerateTasksResult | null>(null);

  orgUrl = signal('');
  project = signal('');

  expandedIds = signal<Set<number>>(new Set());
  loadingTasksFor = signal<Set<number>>(new Set());
  childTasksByParent = signal<Map<number, WorkItemTask[]>>(new Map());

  upcomingSprints = computed(() => {
    const startOfToday = new Date();
    startOfToday.setHours(0, 0, 0, 0);
    return this.sprints().filter((s) => !s.finishDate || new Date(s.finishDate) >= startOfToday);
  });

  totalCount = computed(() => this.selectedUsIds().size * this.selectedSprintPaths().size);

  previewItems = computed<PreviewItem[]>(() => {
    const stories = this.userStories().filter((us) => this.selectedUsIds().has(us.id));
    const paths = [...this.selectedSprintPaths()];
    const items: PreviewItem[] = [];

    for (const us of stories) {
      for (const path of paths) {
        items.push({ title: this.buildTitle(us.title, path), assignedTo: us.assignedTo });
      }
    }

    return items;
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
    this.selectedSprintPaths.set(new Set());
    this.loadSprints();
  }

  loadSprints(): void {
    if (!this.selectedTeamName()) {
      return;
    }

    this.api.getSprints(this.selectedTeamName()).subscribe({
      next: (sprints) => this.sprints.set(sprints),
      error: () => {
        // Deixa a lista de sprints vazia - o card de USs ainda funciona sem ela.
      },
    });
  }

  loadUserStories(): void {
    const id = Number(this.parentId());
    if (!id) {
      this.parentError.set('Informe um ID de work item válido.');
      return;
    }

    this.loadingParent.set(true);
    this.parentError.set('');
    this.result.set(null);

    this.api.getParentUserStories(id).subscribe({
      next: (stories) => {
        this.userStories.set(stories);
        this.selectedUsIds.set(new Set(stories.map((s) => s.id)));
        this.loadingParent.set(false);
        if (stories.length === 0) {
          this.parentError.set('Nenhuma User Story encontrada nesse parent.');
        }
      },
      error: (err) => {
        this.loadingParent.set(false);
        this.userStories.set([]);
        this.parentError.set(err?.error?.message ?? 'Erro ao carregar o parent.');
      },
    });
  }

  toggleUs(id: number, checked: boolean): void {
    const ids = new Set(this.selectedUsIds());
    checked ? ids.add(id) : ids.delete(id);
    this.selectedUsIds.set(ids);
  }

  closeTask(task: WorkItemTask, parentId: number): void {
    const confirmed = window.confirm(`Isso vai fechar a task "${task.title}" (#${task.id}) no Azure DevOps. Continuar?`);
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
        this.loadChildTasks(parentId);
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

  toggleSprint(path: string, checked: boolean): void {
    const paths = new Set(this.selectedSprintPaths());
    checked ? paths.add(path) : paths.delete(path);
    this.selectedSprintPaths.set(paths);
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

    if (this.childTasksByParent().has(id)) {
      return;
    }

    this.loadChildTasks(id);
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

  buildTitle(usTitle: string, iterationPath: string): string {
    const segments = iterationPath.split('\\').filter(Boolean);
    const sprint = segments.length > 0 ? segments[segments.length - 1] : iterationPath;
    const release = segments.length > 1 ? segments[segments.length - 2] : null;
    return release ? `${usTitle} - ${release} - ${sprint}` : `${usTitle} - ${sprint}`;
  }

  generate(): void {
    const userStoryIds = [...this.selectedUsIds()];
    const iterationPaths = [...this.selectedSprintPaths()];
    if (userStoryIds.length === 0 || iterationPaths.length === 0) {
      return;
    }

    this.generating.set(true);
    this.errorMessage.set('');

    this.api.generateTasksFromParent({ userStoryIds, iterationPaths }).subscribe({
      next: (result) => {
        this.result.set(result);
        this.generating.set(false);
      },
      error: (err) => {
        this.generating.set(false);
        this.errorMessage.set(err?.error?.message ?? 'Erro ao gerar tasks.');
      },
    });
  }
}

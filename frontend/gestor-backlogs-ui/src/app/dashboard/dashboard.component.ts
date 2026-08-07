import { Component, OnInit, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../services/api.service';
import { Iteration, SprintDashboard, TeamConfig } from '../models/api-models';

interface CompositionBarVm {
  plannedPct: number;
  outOfSprintPct: number;
  plannedLabel: string;
  outOfSprintLabel: string;
}

interface TrendBarVm {
  label: string;
  isCurrent: boolean;
  pctOutOfSprint: number;
  plannedSharePct: number;
  outOfSprintSharePct: number;
  barHeightPct: number;
}

interface AnalystBarVm {
  name: string;
  totalLabel: string;
  fillPct: number;
  plannedSharePct: number;
  outOfSprintSharePct: number;
}

interface AnalystRowVm {
  name: string;
  plannedItems: number;
  outOfSprintItems: number;
  pctOutOfSprint: number;
  hours: number;
  donePct: number;
  doneClass: 'good' | 'warn' | 'bad';
}

function safeDiv(numerator: number, denominator: number): number {
  return denominator > 0 ? numerator / denominator : 0;
}

function completionClass(pct: number): 'good' | 'warn' | 'bad' {
  if (pct >= 75) {
    return 'good';
  }
  return pct >= 50 ? 'warn' : 'bad';
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './dashboard.component.html',
  styles: ':host { display: contents; }',
})
export class DashboardComponent implements OnInit {
  teams = signal<TeamConfig[]>([]);
  selectedTeamName = signal<string>('');

  sprints = signal<Iteration[]>([]);
  selectedIterationPath = signal<string>('');

  dashboard = signal<SprintDashboard | null>(null);
  loadingSprints = signal(false);
  loadingDashboard = signal(false);
  hasLoadedOnce = signal(false);
  errorMessage = signal('');

  selectedTeam = computed(() => this.teams().find((t) => t.name === this.selectedTeamName()) ?? null);

  totalItems = computed(() => {
    const d = this.dashboard();
    return d ? d.plannedItems + d.outOfSprintItems : 0;
  });

  totalHours = computed(() => {
    const d = this.dashboard();
    return d ? d.plannedHours + d.outOfSprintHours : 0;
  });

  pctOutOfSprintItems = computed(() => {
    const d = this.dashboard();
    return d ? Math.round(safeDiv(d.outOfSprintItems, this.totalItems()) * 100) : 0;
  });

  plannedDonePct = computed(() => {
    const d = this.dashboard();
    return d ? Math.round(safeDiv(d.plannedDoneItems, d.plannedItems) * 100) : 0;
  });

  outOfSprintDonePct = computed(() => {
    const d = this.dashboard();
    return d ? Math.round(safeDiv(d.outOfSprintDoneItems, d.outOfSprintItems) * 100) : 0;
  });

  // Delta do % fora da sprint (em itens) vs a sprint anterior da tendência.
  pctOutOfSprintDelta = computed(() => {
    const trend = this.dashboard()?.trend ?? [];
    if (trend.length < 2) {
      return null;
    }
    const current = trend[trend.length - 1];
    const previous = trend[trend.length - 2];
    const currentPct = safeDiv(current.outOfSprintItems, current.plannedItems + current.outOfSprintItems) * 100;
    const previousPct = safeDiv(previous.outOfSprintItems, previous.plannedItems + previous.outOfSprintItems) * 100;
    return Math.round(currentPct - previousPct);
  });

  compositionByItems = computed<CompositionBarVm>(() => {
    const d = this.dashboard();
    if (!d) {
      return { plannedPct: 0, outOfSprintPct: 0, plannedLabel: '', outOfSprintLabel: '' };
    }
    const total = this.totalItems();
    const plannedPct = safeDiv(d.plannedItems, total) * 100;
    return {
      plannedPct,
      outOfSprintPct: 100 - plannedPct,
      plannedLabel: `${d.plannedItems} (${Math.round(plannedPct)}%)`,
      outOfSprintLabel: `${d.outOfSprintItems} (${Math.round(100 - plannedPct)}%)`,
    };
  });

  compositionByHours = computed<CompositionBarVm>(() => {
    const d = this.dashboard();
    if (!d) {
      return { plannedPct: 0, outOfSprintPct: 0, plannedLabel: '', outOfSprintLabel: '' };
    }
    const total = this.totalHours();
    const plannedPct = safeDiv(d.plannedHours, total) * 100;
    return {
      plannedPct,
      outOfSprintPct: 100 - plannedPct,
      plannedLabel: `${d.plannedHours}h (${Math.round(plannedPct)}%)`,
      outOfSprintLabel: `${d.outOfSprintHours}h (${Math.round(100 - plannedPct)}%)`,
    };
  });

  trendByItems = computed<TrendBarVm[]>(() => this.buildTrendBars((p) => p.plannedItems, (p) => p.outOfSprintItems));
  trendByHours = computed<TrendBarVm[]>(() => this.buildTrendBars((p) => p.plannedHours, (p) => p.outOfSprintHours));

  analystBars = computed<AnalystBarVm[]>(() => {
    const analysts = this.dashboard()?.analysts ?? [];
    const totals = analysts.map((a) => a.plannedItems + a.outOfSprintItems);
    const max = Math.max(1, ...totals) * 1.05;

    return [...analysts]
      .sort((a, b) => (b.plannedItems + b.outOfSprintItems) - (a.plannedItems + a.outOfSprintItems))
      .map((a) => {
        const total = a.plannedItems + a.outOfSprintItems;
        const hours = a.plannedHours + a.outOfSprintHours;
        return {
          name: a.name,
          totalLabel: `${total} (${hours}h)`,
          fillPct: safeDiv(total, max) * 100,
          plannedSharePct: safeDiv(a.plannedItems, total) * 100,
          outOfSprintSharePct: safeDiv(a.outOfSprintItems, total) * 100,
        };
      });
  });

  analystTable = computed<AnalystRowVm[]>(() => {
    const analysts = this.dashboard()?.analysts ?? [];
    return [...analysts]
      .map((a) => {
        const total = a.plannedItems + a.outOfSprintItems;
        const donePct = Math.round(safeDiv(a.doneItems, a.totalItems) * 100);
        return {
          name: a.name,
          plannedItems: a.plannedItems,
          outOfSprintItems: a.outOfSprintItems,
          pctOutOfSprint: Math.round(safeDiv(a.outOfSprintItems, total) * 100),
          hours: a.plannedHours + a.outOfSprintHours,
          donePct,
          doneClass: completionClass(donePct),
        };
      })
      .sort((a, b) => a.donePct - b.donePct);
  });

  constructor(private readonly api: ApiService) {}

  ngOnInit(): void {
    this.api.getConnectionSettings().subscribe({
      next: (settings) => {
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

  onTeamChange(name: string): void {
    this.selectedTeamName.set(name);
    this.sprints.set([]);
    this.selectedIterationPath.set('');
    this.dashboard.set(null);
    this.hasLoadedOnce.set(false);
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
    this.dashboard.set(null);
    this.hasLoadedOnce.set(false);
  }

  loadDashboard(): void {
    const team = this.selectedTeam();
    if (!team || !this.selectedIterationPath()) {
      return;
    }

    this.hasLoadedOnce.set(true);
    this.loadingDashboard.set(true);
    this.errorMessage.set('');

    this.api.getSprintDashboard(team.name, team.areaPath || null, this.selectedIterationPath()).subscribe({
      next: (dashboard) => {
        this.dashboard.set(dashboard);
        this.loadingDashboard.set(false);
      },
      error: (err) => {
        this.loadingDashboard.set(false);
        this.errorMessage.set(err?.error?.message ?? 'Erro ao carregar o dashboard da sprint.');
      },
    });
  }

  stateBadgeClass(state: string | null): string {
    if (!state) {
      return '';
    }
    const normalized = state.toLowerCase();
    if (normalized === 'resolved' || normalized === 'closed') {
      return 'success';
    }
    return normalized === 'active' ? 'warning' : '';
  }

  private buildTrendBars(selectPlanned: (p: SprintDashboard['trend'][number]) => number, selectOutOfSprint: (p: SprintDashboard['trend'][number]) => number): TrendBarVm[] {
    const trend = this.dashboard()?.trend ?? [];
    const totals = trend.map((p) => selectPlanned(p) + selectOutOfSprint(p));
    const max = Math.max(1, ...totals) * 1.15;

    return trend.map((p) => {
      const planned = selectPlanned(p);
      const outOfSprint = selectOutOfSprint(p);
      const total = planned + outOfSprint;
      return {
        label: p.label,
        isCurrent: p.isCurrent,
        pctOutOfSprint: Math.round(safeDiv(outOfSprint, total) * 100),
        plannedSharePct: safeDiv(planned, total) * 100,
        outOfSprintSharePct: safeDiv(outOfSprint, total) * 100,
        barHeightPct: safeDiv(total, max) * 100,
      };
    });
  }
}

import { Component, OnDestroy, OnInit, Signal, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Subscription, interval, switchMap } from 'rxjs';
import { ApiService } from '../services/api.service';
import { AuthMode, ConnectionSettings, EntraDeviceCodeInfo, TeamConfig } from '../models/api-models';
import { Theme, ThemeService } from '../services/theme.service';

@Component({
  selector: 'app-config',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './config.component.html',
  styles: ':host { display: contents; }',
})
export class ConfigComponent implements OnInit, OnDestroy {
  model: ConnectionSettings = {
    organizationUrl: 'https://dev.azure.com/Ailos',
    project: 'Ailos',
    teams: [{ name: '', areaPath: '' }],
    authMode: 'Sso',
    personalAccessToken: '',
  };

  hasStoredToken = signal(false);
  status = signal<'idle' | 'testing' | 'success' | 'error'>('idle');
  statusMessage = signal('');
  entraDeviceCode = signal<EntraDeviceCodeInfo | null>(null);

  theme: Signal<Theme>;

  private entraPollSubscription?: Subscription;

  constructor(
    private readonly api: ApiService,
    private readonly router: Router,
    private readonly themeService: ThemeService,
  ) {
    this.theme = this.themeService.theme;
  }

  setTheme(value: Theme): void {
    this.themeService.setTheme(value);
  }

  ngOnInit(): void {
    this.api.getConnectionSettings().subscribe({
      next: (settings) => {
        this.model.organizationUrl = settings.organizationUrl;
        this.model.project = settings.project;
        this.model.teams = settings.teams.length > 0 ? settings.teams : [{ name: '', areaPath: '' }];
        this.model.authMode = settings.authMode;
        this.hasStoredToken.set(settings.hasToken);
      },
      error: () => {
        // Nenhuma configuração salva ainda - mantém os valores padrão do formulário.
      },
    });
  }

  ngOnDestroy(): void {
    this.entraPollSubscription?.unsubscribe();
  }

  setAuthMode(mode: AuthMode): void {
    this.model.authMode = mode;
    this.entraDeviceCode.set(null);
    this.status.set('idle');
    this.statusMessage.set('');
  }

  addTeam(): void {
    this.model.teams = [...this.model.teams, { name: '', areaPath: '' }];
  }

  removeTeam(index: number): void {
    this.model.teams = this.model.teams.filter((_, i) => i !== index);
  }

  trackByIndex(index: number): number {
    return index;
  }

  save(): void {
    const teams = this.buildTeams();
    if (!teams) {
      return;
    }

    this.status.set('testing');
    this.statusMessage.set('');
    this.finishSave(teams);
  }

  loginWithMicrosoft(): void {
    const teams = this.buildTeams();
    if (!teams) {
      return;
    }

    this.status.set('testing');
    this.statusMessage.set('');
    this.entraDeviceCode.set(null);

    this.api.startEntraLogin().subscribe({
      next: (info) => {
        this.entraDeviceCode.set(info);
        this.pollEntraLoginStatus(teams);
      },
      error: (err) => {
        this.status.set('error');
        this.statusMessage.set(err?.error?.message ?? 'Não foi possível iniciar o login com a Microsoft.');
      },
    });
  }

  logoutMicrosoft(): void {
    this.api.logoutEntra().subscribe(() => {
      this.hasStoredToken.set(false);
      this.status.set('idle');
      this.statusMessage.set('');
    });
  }

  private pollEntraLoginStatus(teams: TeamConfig[]): void {
    this.entraPollSubscription?.unsubscribe();
    this.entraPollSubscription = interval(2000)
      .pipe(switchMap(() => this.api.getEntraLoginStatus()))
      .subscribe({
        next: (loginStatus) => {
          if (loginStatus.status === 'success') {
            this.entraPollSubscription?.unsubscribe();
            this.entraDeviceCode.set(null);
            this.finishSave(teams);
          } else if (loginStatus.status === 'error') {
            this.entraPollSubscription?.unsubscribe();
            this.entraDeviceCode.set(null);
            this.status.set('error');
            this.statusMessage.set(loginStatus.message ?? 'Não foi possível concluir o login com a Microsoft.');
          }
        },
        error: () => {
          this.entraPollSubscription?.unsubscribe();
          this.entraDeviceCode.set(null);
          this.status.set('error');
          this.statusMessage.set('Não foi possível verificar o status do login.');
        },
      });
  }

  private buildTeams(): TeamConfig[] | null {
    const teams = this.model.teams
      .map((t): TeamConfig => ({ name: t.name.trim(), areaPath: t.areaPath.trim() }))
      .filter((t) => t.name.length > 0);

    if (teams.length === 0) {
      this.status.set('error');
      this.statusMessage.set('Cadastre pelo menos um time.');
      return null;
    }

    return teams;
  }

  private finishSave(teams: TeamConfig[]): void {
    this.api.saveConnectionSettings({ ...this.model, teams }).subscribe({
      next: () => {
        this.api.testConnection().subscribe({
          next: (result) => {
            if (result.success) {
              this.status.set('success');
              this.statusMessage.set('Conexão validada com sucesso.');
              this.model.personalAccessToken = '';
              this.hasStoredToken.set(true);
              this.router.navigate(['/work-items']);
            } else {
              this.status.set('error');
              this.statusMessage.set(result.message ?? 'Não foi possível conectar.');
            }
          },
          error: (err) => {
            this.status.set('error');
            this.statusMessage.set(err?.error?.message ?? 'Não foi possível conectar.');
          },
        });
      },
      error: (err) => {
        this.status.set('error');
        this.statusMessage.set(err?.error?.message ?? 'Erro ao salvar configuração.');
      },
    });
  }
}

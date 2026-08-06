import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ApiService } from '../services/api.service';
import { ConnectionSettings, TeamConfig } from '../models/api-models';

@Component({
  selector: 'app-config',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './config.component.html',
  styles: ':host { display: contents; }',
})
export class ConfigComponent implements OnInit {
  model: ConnectionSettings = {
    organizationUrl: 'https://dev.azure.com/Ailos',
    project: 'Ailos',
    teams: [{ name: '', areaPath: '' }],
    personalAccessToken: '',
  };

  hasStoredToken = signal(false);
  status = signal<'idle' | 'testing' | 'success' | 'error'>('idle');
  statusMessage = signal('');

  constructor(
    private readonly api: ApiService,
    private readonly router: Router,
  ) {}

  ngOnInit(): void {
    this.api.getConnectionSettings().subscribe({
      next: (settings) => {
        this.model.organizationUrl = settings.organizationUrl;
        this.model.project = settings.project;
        this.model.teams = settings.teams.length > 0 ? settings.teams : [{ name: '', areaPath: '' }];
        this.hasStoredToken.set(settings.hasToken);
      },
      error: () => {
        // Nenhuma configuração salva ainda - mantém os valores padrão do formulário.
      },
    });
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
    this.status.set('testing');
    this.statusMessage.set('');

    const teams = this.model.teams
      .map((t): TeamConfig => ({ name: t.name.trim(), areaPath: t.areaPath.trim() }))
      .filter((t) => t.name.length > 0);

    if (teams.length === 0) {
      this.status.set('error');
      this.statusMessage.set('Cadastre pelo menos um time.');
      return;
    }

    this.api.saveConnectionSettings({ ...this.model, teams }).subscribe({
      next: () => {
        this.api.testConnection().subscribe({
          next: (result) => {
            if (result.success) {
              this.status.set('success');
              this.statusMessage.set('Conexão validada com sucesso.');
              this.model.personalAccessToken = '';
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

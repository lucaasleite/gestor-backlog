import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';
import { ApiService } from '../services/api.service';
import { ConnectionSettings } from '../models/api-models';

@Component({
  selector: 'app-config',
  standalone: true,
  imports: [
    FormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    MatIconModule,
  ],
  templateUrl: './config.component.html',
  styleUrl: './config.component.css',
})
export class ConfigComponent implements OnInit {
  model: ConnectionSettings = {
    organizationUrl: 'https://dev.azure.com/Ailos',
    project: 'Ailos',
    team: '',
    areaPath: '',
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
        this.model.team = settings.team;
        this.model.areaPath = settings.areaPath ?? '';
        this.hasStoredToken.set(settings.hasToken);
      },
      error: () => {
        // Nenhuma configuração salva ainda - mantém os valores padrão do formulário.
      },
    });
  }

  save(): void {
    this.status.set('testing');
    this.statusMessage.set('');

    this.api.saveConnectionSettings(this.model).subscribe({
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

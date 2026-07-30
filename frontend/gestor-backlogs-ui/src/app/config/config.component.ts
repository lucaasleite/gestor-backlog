import { Component, EventEmitter, OnInit, Output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../services/api.service';
import { ConnectionSettings } from '../models/api-models';

@Component({
  selector: 'app-config',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './config.component.html',
  styleUrl: './config.component.css',
})
export class ConfigComponent implements OnInit {
  @Output() saved = new EventEmitter<void>();

  model: ConnectionSettings = {
    organizationUrl: 'https://dev.azure.com/Ailos',
    project: 'Ailos',
    team: '',
    personalAccessToken: '',
  };

  hasStoredToken = signal(false);
  status = signal<'idle' | 'testing' | 'success' | 'error'>('idle');
  statusMessage = signal('');

  constructor(private readonly api: ApiService) {}

  ngOnInit(): void {
    this.api.getConnectionSettings().subscribe({
      next: (settings) => {
        this.model.organizationUrl = settings.organizationUrl;
        this.model.project = settings.project;
        this.model.team = settings.team;
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
              this.saved.emit();
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

import { Component, OnInit, signal } from '@angular/core';
import { ConfigComponent } from './config/config.component';
import { WorkItemsComponent } from './work-items/work-items.component';
import { ApiService } from './services/api.service';

type View = 'loading' | 'config' | 'work-items';

@Component({
  selector: 'app-root',
  imports: [ConfigComponent, WorkItemsComponent],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App implements OnInit {
  view = signal<View>('loading');

  constructor(private readonly api: ApiService) {}

  ngOnInit(): void {
    this.api.getConnectionSettings().subscribe({
      next: (settings) => this.view.set(settings.hasToken ? 'work-items' : 'config'),
      error: () => this.view.set('config'),
    });
  }

  onConfigSaved(): void {
    this.view.set('work-items');
  }

  onOpenConfig(): void {
    this.view.set('config');
  }
}

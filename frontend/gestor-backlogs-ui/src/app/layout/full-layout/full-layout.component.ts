import { Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { SidebarComponent } from '../sidebar/sidebar.component';
import { Theme, ThemeService } from '../../services/theme.service';

@Component({
  selector: 'app-full-layout',
  standalone: true,
  imports: [RouterOutlet, SidebarComponent],
  templateUrl: './full-layout.component.html',
  styles: ':host { display: contents; }',
})
export class FullLayoutComponent {
  sidebarOpen = signal(false);
  theme: () => Theme;

  constructor(private readonly themeService: ThemeService) {
    this.theme = this.themeService.theme;
  }

  toggleTheme(): void {
    this.themeService.setTheme(this.themeService.theme() === 'dark' ? 'light' : 'dark');
  }

  toggleSidebar(): void {
    this.sidebarOpen.update((open) => !open);
  }

  closeSidebar(): void {
    this.sidebarOpen.set(false);
  }
}

import { Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { SidebarComponent } from '../sidebar/sidebar.component';

@Component({
  selector: 'app-full-layout',
  standalone: true,
  imports: [RouterOutlet, SidebarComponent],
  templateUrl: './full-layout.component.html',
  styles: ':host { display: contents; }',
})
export class FullLayoutComponent {
  sidebarOpen = signal(false);

  toggleSidebar(): void {
    this.sidebarOpen.update((open) => !open);
  }

  closeSidebar(): void {
    this.sidebarOpen.set(false);
  }
}

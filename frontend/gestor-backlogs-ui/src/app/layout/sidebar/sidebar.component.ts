import { Component, EventEmitter, Output } from '@angular/core';
import { AppNavItemComponent } from './nav-item/nav-item.component';
import { navItems } from './sidebar-data';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [AppNavItemComponent],
  templateUrl: './sidebar.component.html',
  styles: ':host { display: contents; }',
})
export class SidebarComponent {
  navItems = navItems;
  @Output() navigated = new EventEmitter<void>();
}

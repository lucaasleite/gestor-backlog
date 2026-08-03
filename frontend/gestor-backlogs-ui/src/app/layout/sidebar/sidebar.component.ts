import { Component } from '@angular/core';
import { MatListModule } from '@angular/material/list';
import { AppNavItemComponent } from './nav-item/nav-item.component';
import { navItems } from './sidebar-data';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [MatListModule, AppNavItemComponent],
  templateUrl: './sidebar.component.html',
})
export class SidebarComponent {
  navItems = navItems;
}

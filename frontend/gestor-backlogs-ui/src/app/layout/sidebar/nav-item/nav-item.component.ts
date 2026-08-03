import { Component, Input } from '@angular/core';
import { RouterModule } from '@angular/router';
import { MatListModule } from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';
import { NavItem } from './nav-item';

@Component({
  selector: 'app-nav-item',
  standalone: true,
  imports: [RouterModule, MatListModule, MatIconModule],
  templateUrl: './nav-item.component.html',
})
export class AppNavItemComponent {
  @Input({ required: true }) item!: NavItem;
}

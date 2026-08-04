import { Component, Input } from '@angular/core';
import { RouterModule } from '@angular/router';
import { NavItem } from './nav-item';

@Component({
  selector: 'app-nav-item',
  standalone: true,
  imports: [RouterModule],
  templateUrl: './nav-item.component.html',
  styles: ':host { display: block; }',
})
export class AppNavItemComponent {
  @Input({ required: true }) item!: NavItem;
}

import { BreakpointObserver } from '@angular/cdk/layout';
import { Component, ViewChild } from '@angular/core';
import { MatSidenav, MatSidenavModule } from '@angular/material/sidenav';
import { RouterOutlet } from '@angular/router';
import { HeaderComponent } from '../header/header.component';
import { SidebarComponent } from '../sidebar/sidebar.component';

const MOBILE_VIEW = 'screen and (max-width: 1023px)';

@Component({
  selector: 'app-full-layout',
  standalone: true,
  imports: [MatSidenavModule, RouterOutlet, HeaderComponent, SidebarComponent],
  templateUrl: './full-layout.component.html',
})
export class FullLayoutComponent {
  @ViewChild('sidenav') sidenav!: MatSidenav;
  isOver = false;

  constructor(breakpointObserver: BreakpointObserver) {
    breakpointObserver.observe([MOBILE_VIEW]).subscribe((state) => {
      this.isOver = state.matches;
    });
  }
}

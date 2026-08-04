import { Routes } from '@angular/router';
import { FullLayoutComponent } from './layout/full-layout/full-layout.component';
import { ConfigComponent } from './config/config.component';
import { WorkItemsComponent } from './work-items/work-items.component';
import { ParentTasksComponent } from './parent-tasks/parent-tasks.component';
import { configuredGuard } from './guards/configured.guard';

export const routes: Routes = [
  {
    path: '',
    component: FullLayoutComponent,
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'work-items' },
      { path: 'config', component: ConfigComponent },
      { path: 'work-items', component: WorkItemsComponent, canActivate: [configuredGuard] },
      { path: 'parent-tasks', component: ParentTasksComponent, canActivate: [configuredGuard] },
    ],
  },
  { path: '**', redirectTo: 'work-items' },
];

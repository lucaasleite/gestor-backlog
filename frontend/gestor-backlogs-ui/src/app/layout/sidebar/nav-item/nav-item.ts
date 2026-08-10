export type NavIcon = 'config' | 'dashboard' | 'work-items' | 'parent-tasks' | 'single-item';

export interface NavItem {
  displayName: string;
  route: string;
  icon: NavIcon;
}

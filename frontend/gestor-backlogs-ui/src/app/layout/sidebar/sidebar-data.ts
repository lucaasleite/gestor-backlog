import { NavItem } from './nav-item/nav-item';

export const navItems: NavItem[] = [
  { displayName: 'Configuração', route: '/config', icon: 'config' },
  { displayName: 'Dashboard', route: '/dashboard', icon: 'dashboard' },
  { displayName: 'Work Items', route: '/work-items', icon: 'work-items' },
  { displayName: 'Tasks por Parent', route: '/parent-tasks', icon: 'parent-tasks' },
  { displayName: 'Tasks por Item', route: '/single-item', icon: 'single-item' },
];

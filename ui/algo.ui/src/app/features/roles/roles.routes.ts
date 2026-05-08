import { Routes } from '@angular/router';

export const ROLES_ROUTES: Routes = [
  {
    path: '',
    title: 'Roles | algo.ui',
    loadComponent: () =>
      import('./pages/roles-list/roles-list').then((m) => m.RolesList)
  }
];

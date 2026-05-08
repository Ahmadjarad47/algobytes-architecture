import { Routes } from '@angular/router';

export const DASHBOARD_ROUTES: Routes = [
  {
    path: '',
    title: 'Dashboard | algo.ui',
    loadComponent: () =>
      import('./pages/overview/overview').then((m) => m.Overview)
  }
];

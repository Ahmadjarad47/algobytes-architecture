import { Routes } from '@angular/router';

export const LOGS_ROUTES: Routes = [
  {
    path: '',
    title: 'Logs | algo.ui',
    loadComponent: () =>
      import('./pages/logs-list/logs-list').then((m) => m.LogsList)
  }
];

import { Routes } from '@angular/router';

export const ERROR_LOGS_ROUTES: Routes = [
  {
    path: '',
    title: 'Error Logs | algo.ui',
    loadComponent: () =>
      import('./pages/error-logs-list/error-logs-list').then(
        (m) => m.ErrorLogsList
      )
  }
];

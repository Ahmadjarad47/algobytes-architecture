import { Routes } from '@angular/router';

export const REPORTS_ROUTES: Routes = [
  {
    path: '',
    title: 'Reports | algo.ui',
    loadComponent: () =>
      import('./pages/reports-home/reports-home').then((m) => m.ReportsHome)
  }
];

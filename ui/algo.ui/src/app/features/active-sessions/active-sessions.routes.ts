import { Routes } from '@angular/router';

export const ACTIVE_SESSIONS_ROUTES: Routes = [
  {
    path: '',
    title: 'Active Sessions | algo.ui',
    loadComponent: () =>
      import('./pages/active-sessions-list/active-sessions-list').then(
        (m) => m.ActiveSessionsList
      )
  }
];

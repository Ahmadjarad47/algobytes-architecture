import { Routes } from '@angular/router';

export const USERS_ROUTES: Routes = [
  {
    path: '',
    title: 'Users | algo.ui',
    loadComponent: () =>
      import('./pages/users-list/users-list').then((m) => m.UsersList)
  }
];

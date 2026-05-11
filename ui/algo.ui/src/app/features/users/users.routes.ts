import { Routes } from '@angular/router';

export const USERS_ROUTES: Routes = [
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'directory'
  },
  {
    path: 'directory',
    title: 'Users | algo.ui',
    loadComponent: () =>
      import('./pages/users-list/users-list').then((m) => m.UsersList)
  },
  {
    path: 'chat',
    title: 'Users Chat | algo.ui',
    loadComponent: () =>
      import('./pages/users-chat/users-chat').then((m) => m.UsersChat)
  }
];

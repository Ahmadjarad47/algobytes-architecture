import { Routes } from '@angular/router';

export const AUTH_ROUTES: Routes = [
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'login'
  },
  {
    path: 'login',
    title: 'Login | algo.ui',
    loadComponent: () =>
      import('./pages/login/login').then((m) => m.Login)
  },
  {
    path: 'register',
    title: 'Register | algo.ui',
    loadComponent: () =>
      import('./pages/register/register').then((m) => m.Register)
  }
];

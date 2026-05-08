import { Routes } from '@angular/router';

import { authGuard } from './core/guards/auth.guard';
import { guestGuard } from './core/guards/guest.guard';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'dashboard'
  },
  {
    path: 'auth',
    canActivate: [guestGuard],
    loadComponent: () =>
      import('./layouts/auth-layout/auth-layout').then((m) => m.AuthLayout),
    loadChildren: () =>
      import('./features/auth/auth.routes').then((m) => m.AUTH_ROUTES)
  },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./layouts/dashboard-layout/dashboard-layout').then(
        (m) => m.DashboardLayout
      ),
    children: [
      {
        path: 'dashboard',
        loadChildren: () =>
          import('./features/dashboard/dashboard.routes').then(
            (m) => m.DASHBOARD_ROUTES
          )
      },
      {
        path: 'users',
        loadChildren: () =>
          import('./features/users/users.routes').then((m) => m.USERS_ROUTES)
      },
      {
        path: 'roles',
        loadChildren: () =>
          import('./features/roles/roles.routes').then((m) => m.ROLES_ROUTES)
      },
      {
        path: 'access-policies',
        loadChildren: () =>
          import('./features/access-policies/access-policies.routes').then(
            (m) => m.ACCESS_POLICIES_ROUTES
          )
      },
      {
        path: 'logs',
        loadChildren: () =>
          import('./features/logs/logs.routes').then((m) => m.LOGS_ROUTES)
      },
      {
        path: 'error-logs',
        loadChildren: () =>
          import('./features/error-logs/error-logs.routes').then(
            (m) => m.ERROR_LOGS_ROUTES
          )
      },
      {
        path: 'settings',
        loadChildren: () =>
          import('./features/settings/settings.routes').then(
            (m) => m.SETTINGS_ROUTES
          )
      },
      {
        path: 'reports',
        loadChildren: () =>
          import('./features/reports/reports.routes').then(
            (m) => m.REPORTS_ROUTES
          )
      }
    ]
  },
  {
    path: '**',
    redirectTo: 'dashboard'
  }
];

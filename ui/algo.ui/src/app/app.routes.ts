import { Routes } from '@angular/router';

import { authGuard } from './core/guards/auth.guard';
import { guestGuard } from './core/guards/guest.guard';
import { landingRouteGuard } from './core/guards/landing-route.guard';
import { permissionGuard } from './core/guards/permission.guard';
import { DashboardOverviewReadPermissions, Permissions } from './core/permissions/permission.catalog';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    canActivate: [landingRouteGuard],
    children: []
  },
  {
    path: 'access-denied',
    title: 'Access denied | algo.ui',
    loadComponent: () =>
      import('./features/auth/pages/access-denied/access-denied').then((m) => m.AccessDenied)
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
        canActivate: [permissionGuard],
        data: { permission: { any: DashboardOverviewReadPermissions } },
        loadChildren: () =>
          import('./features/dashboard/dashboard.routes').then(
            (m) => m.DASHBOARD_ROUTES
          )
      },
      {
        path: 'users',
        canActivate: [permissionGuard],
        data: { permission: { any: [Permissions.users.read] } },
        loadChildren: () =>
          import('./features/users/users.routes').then((m) => m.USERS_ROUTES)
      },
      {
        path: 'roles',
        canActivate: [permissionGuard],
        data: { permission: { any: [Permissions.roles.read] } },
        loadChildren: () =>
          import('./features/roles/roles.routes').then((m) => m.ROLES_ROUTES)
      },
      {
        path: 'access-policies',
        canActivate: [permissionGuard],
        data: { permission: { any: [Permissions.accessPolicies.read] } },
        loadChildren: () =>
          import('./features/access-policies/access-policies.routes').then(
            (m) => m.ACCESS_POLICIES_ROUTES
          )
      },
      {
        path: 'active-sessions',
        canActivate: [permissionGuard],
        data: { permission: { any: [Permissions.sessions.read] } },
        loadChildren: () =>
          import('./features/active-sessions/active-sessions.routes').then(
            (m) => m.ACTIVE_SESSIONS_ROUTES
          )
      },
      {
        path: 'logs',
        canActivate: [permissionGuard],
        data: { permission: { any: [Permissions.logs.read] } },
        loadChildren: () =>
          import('./features/logs/logs.routes').then((m) => m.LOGS_ROUTES)
      },
      {
        path: 'error-logs',
        canActivate: [permissionGuard],
        data: { permission: { any: [Permissions.errorLogs.read] } },
        loadChildren: () =>
          import('./features/error-logs/error-logs.routes').then(
            (m) => m.ERROR_LOGS_ROUTES
          )
      },
      {
        path: 'settings',
        canActivate: [permissionGuard],
        data: { permission: { any: [Permissions.settings.read] } },
        loadChildren: () =>
          import('./features/settings/settings.routes').then(
            (m) => m.SETTINGS_ROUTES
          )
      },
      {
        path: 'reports',
        canActivate: [permissionGuard],
        data: { permission: { any: [Permissions.reports.read] } },
        loadChildren: () =>
          import('./features/reports/reports.routes').then(
            (m) => m.REPORTS_ROUTES
          )
      }
    ]
  },
  {
    path: '**',
    canActivate: [landingRouteGuard],
    children: []
  }
];

import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { Permissions } from '../permissions/permission.catalog';
import { PermissionService } from '../permissions/permission.service';
import { AuthService } from '../services/auth.service';

function resolveLandingRoute(): string {
  const authService = inject(AuthService);
  const permissionService = inject(PermissionService);

  if (!authService.isAuthenticated()) {
    return '/auth/login';
  }

  if (permissionService.can({ any: [Permissions.users.read] })) {
    return '/users';
  }

  if (permissionService.can({ any: [Permissions.roles.read] })) {
    return '/roles';
  }

  if (permissionService.can({ any: [Permissions.accessPolicies.read] })) {
    return '/access-policies';
  }

  if (permissionService.can({ any: [Permissions.sessions.read] })) {
    return '/active-sessions';
  }

  if (permissionService.can({ any: [Permissions.logs.read] })) {
    return '/logs';
  }

  if (permissionService.can({ any: [Permissions.errorLogs.read] })) {
    return '/error-logs';
  }

  if (permissionService.can({ any: [Permissions.settings.read] })) {
    return '/settings';
  }

  if (permissionService.can({ any: [Permissions.reports.read] })) {
    return '/reports';
  }

  if (permissionService.can({ any: [Permissions.roles.read, Permissions.accessPolicies.read, Permissions.sessions.read, Permissions.logs.read, Permissions.errorLogs.read, Permissions.reports.read] })) {
    return '/dashboard';
  }

  return '/access-denied';
}

export const landingRouteGuard: CanActivateFn = () => {
  const router = inject(Router);
  return router.createUrlTree([resolveLandingRoute()]);
};

export const resolveAuthenticatedLandingRoute = (): string => resolveLandingRoute();

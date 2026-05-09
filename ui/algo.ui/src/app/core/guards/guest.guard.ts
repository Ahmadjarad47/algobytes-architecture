import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { Permissions } from '../permissions/permission.catalog';
import { PermissionService } from '../permissions/permission.service';
import { AuthService } from '../services/auth.service';

export const guestGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const permissionService = inject(PermissionService);
  const router = inject(Router);

  const landingRoute =
    permissionService.can({ any: [Permissions.users.read] }) ? '/users' :
    permissionService.can({ any: [Permissions.roles.read] }) ? '/roles' :
    permissionService.can({ any: [Permissions.accessPolicies.read] }) ? '/access-policies' :
    permissionService.can({ any: [Permissions.sessions.read] }) ? '/active-sessions' :
    permissionService.can({ any: [Permissions.logs.read] }) ? '/logs' :
    permissionService.can({ any: [Permissions.errorLogs.read] }) ? '/error-logs' :
    permissionService.can({ any: [Permissions.settings.read] }) ? '/settings' :
    permissionService.can({ any: [Permissions.reports.read] }) ? '/reports' :
    '/access-denied';

  return authService.isAuthenticated()
    ? router.createUrlTree([landingRoute])
    : true;
};

import { inject } from '@angular/core';
import { ActivatedRouteSnapshot, CanActivateFn, Router } from '@angular/router';

import { PermissionService } from '../permissions/permission.service';
import { PermissionGate } from '../permissions/permission.types';

function routePermissionGate(route: ActivatedRouteSnapshot): PermissionGate | undefined {
  return route.data['permission'] as PermissionGate | undefined;
}

export const permissionGuard: CanActivateFn = (route) => {
  const permissions = inject(PermissionService);
  const router = inject(Router);
  const gate = routePermissionGate(route);

  return permissions.can(gate) ? true : router.createUrlTree(['/access-denied']);
};

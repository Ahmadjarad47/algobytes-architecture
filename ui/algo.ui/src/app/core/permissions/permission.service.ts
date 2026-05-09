import { Injectable, computed, inject } from '@angular/core';

import { AuthService } from '../services/auth.service';
import { AppPermission, PermissionGate } from './permission.types';

@Injectable({ providedIn: 'root' })
export class PermissionService {
  private readonly auth = inject(AuthService);

  readonly permissionSet = computed(() => new Set(this.auth.session()?.user?.permissions ?? []));

  has(permission: AppPermission): boolean {
    return this.hasPermissionString(permission.resource, permission.action);
  }

  hasAll(permissions: readonly AppPermission[]): boolean {
    return permissions.every((permission) => this.has(permission));
  }

  hasAny(permissions: readonly AppPermission[]): boolean {
    return permissions.some((permission) => this.has(permission));
  }

  can(gate?: PermissionGate): boolean {
    if (!gate) {
      return true;
    }

    const allAllowed = !gate.all || this.hasAll(gate.all);
    const anyAllowed = !gate.any || this.hasAny(gate.any);
    return allAllowed && anyAllowed;
  }

  private hasPermissionString(resource: string, action: string): boolean {
    const set = this.permissionSet();
    return set.has('*:*') || set.has(`${resource}:*`) || set.has(`${resource}:${action}`);
  }
}

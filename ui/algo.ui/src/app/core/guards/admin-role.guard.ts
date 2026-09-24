import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { AuthService } from '../services/auth.service';

export const adminRoleGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const roles = authService.session()?.user?.roles ?? [];
  const isAdmin = roles.some((role) => role.toLowerCase() === 'admin');

  return isAdmin ? true : router.createUrlTree(['/access-denied']);
};

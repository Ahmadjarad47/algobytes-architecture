import { inject } from '@angular/core';
import { HttpErrorResponse, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';

import { ApiError } from '../models/api-error.model';
import { AuthService } from '../services/auth.service';
import { AppToastService } from '../services/app-toast.service';
import { SessionRealtimeService } from '../services/session-realtime.service';

export const errorInterceptor: HttpInterceptorFn = (request, next) => {
  const toast = inject(AppToastService);
  const authService = inject(AuthService);
  const router = inject(Router);
  const sessionRealtime = inject(SessionRealtimeService);

  return next(request).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse) {
        if (error.status === 401 && shouldRedirectToLogin(request)) {
          sessionRealtime.stop();
          authService.clearSession();

          if (!router.url.startsWith('/auth')) {
            void router.navigateByUrl('/auth/login');
          }
        }

        const message = errorMessage(error);
        const apiError: ApiError = {
          message,
          status: error.status
        };

        toast.error(errorSummary(error), message);

        return throwError(() => apiError);
      }

      return throwError(() => error);
    })
  );
};

function shouldRedirectToLogin(request: HttpRequest<unknown>): boolean {
  const normalizedUrl = request.url.toLowerCase();
  return !normalizedUrl.includes('/auth/login') &&
    !normalizedUrl.includes('/auth/register') &&
    !normalizedUrl.includes('/auth/refresh-token');
}

function errorSummary(error: HttpErrorResponse): string {
  if (error.status === 0) {
    return 'Connection failed';
  }

  if (error.status === 401) {
    return 'Not authorized';
  }

  if (error.status === 403) {
    return 'Access denied';
  }

  if (error.status >= 500) {
    return 'Server error';
  }

  return 'Action failed';
}

function errorMessage(error: HttpErrorResponse): string {
  const payload = error.error;

  if (payload && typeof payload === 'object') {
    const detail = getStringValue(payload, 'detail') ?? getStringValue(payload, 'message');
    if (detail) {
      return detail;
    }

    const errors = (payload as Record<string, unknown>)['errors'];
    if (errors && typeof errors === 'object') {
      const firstError = Object.values(errors as Record<string, unknown>)
        .flatMap((value) => Array.isArray(value) ? value : [value])
        .find((value): value is string => typeof value === 'string' && value.trim().length > 0);

      if (firstError) {
        return firstError;
      }
    }
  }

  if (typeof payload === 'string' && payload.trim().length > 0) {
    return payload;
  }

  return error.statusText || error.message || 'Please try again.';
}

function getStringValue(source: object, key: string): string | undefined {
  const value = (source as Record<string, unknown>)[key];
  return typeof value === 'string' && value.trim().length > 0 ? value : undefined;
}

import { inject } from '@angular/core';
import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';

import { ApiError } from '../models/api-error.model';
import { AppToastService } from '../services/app-toast.service';

export const errorInterceptor: HttpInterceptorFn = (request, next) => {
  const toast = inject(AppToastService);

  return next(request).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse) {
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

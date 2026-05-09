import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { AppConfigService } from '../config/app-config.service';

export type ApiQueryValue =
  | string
  | number
  | boolean
  | null
  | undefined
  | Date;

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);
  private readonly config = inject(AppConfigService);

  get<TResponse>(path: string, query?: object): Observable<TResponse> {
    return this.http.get<TResponse>(this.url(path), {
      params: this.toHttpParams(query)
    });
  }

  post<TResponse, TBody extends object>(
    path: string,
    body: TBody
  ): Observable<TResponse> {
    return this.http.post<TResponse>(this.url(path), body);
  }

  put<TResponse, TBody extends object>(
    path: string,
    body: TBody
  ): Observable<TResponse> {
    return this.http.put<TResponse>(this.url(path), body);
  }

  patch<TResponse, TBody extends object | null = null>(
    path: string,
    body?: TBody
  ): Observable<TResponse> {
    return this.http.patch<TResponse>(this.url(path), body ?? null);
  }

  delete<TResponse>(path: string): Observable<TResponse> {
    return this.http.delete<TResponse>(this.url(path));
  }

  deleteWithBody<TResponse, TBody extends object>(
    path: string,
    body: TBody
  ): Observable<TResponse> {
    return this.http.delete<TResponse>(this.url(path), {
      body
    });
  }

  private url(path: string): string {
    const normalizedPath = path.startsWith('/') ? path : `/${path}`;

    return `${this.config.apiBaseUrl()}${normalizedPath}`;
  }

  private toHttpParams(query?: object): HttpParams | undefined {
    if (!query) {
      return undefined;
    }

    let params = new HttpParams();

    for (const [key, value] of Object.entries(
      query as Record<string, ApiQueryValue>
    )) {
      if (value === null || value === undefined || value === '') {
        continue;
      }

      const normalizedValue =
        value instanceof Date ? value.toISOString() : String(value);

      params = params.set(key, normalizedValue);
    }

    return params;
  }
}

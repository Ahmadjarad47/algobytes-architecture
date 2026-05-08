import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiService } from '../../../core/api/api.service';
import {
  ErrorLogDto,
  ErrorLogsPage,
  ErrorLogsQuery
} from '../models/error-logs.models';

@Injectable({ providedIn: 'root' })
export class ErrorLogsApiService {
  private readonly api = inject(ApiService);

  getErrorLogs(query: ErrorLogsQuery): Observable<ErrorLogsPage> {
    return this.api.get<ErrorLogsPage>('/error-logs', query);
  }

  getErrorLog(id: string): Observable<ErrorLogDto> {
    return this.api.get<ErrorLogDto>(`/error-logs/${id}`);
  }
}

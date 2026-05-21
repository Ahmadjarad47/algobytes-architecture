import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiService } from '../../../core/api/api.service';
import { ApplicationLogDto, LogsPage, LogsQuery } from '../models/logs.models';

@Injectable({ providedIn: 'root' })
export class LogsApiService {
  private readonly api = inject(ApiService);

  getLogs(query: LogsQuery): Observable<LogsPage> {
    return this.api.get<LogsPage>('/Logs', query);
  }

  getLog(id: string): Observable<ApplicationLogDto> {
    return this.api.get<ApplicationLogDto>(`/Logs/${id}`);
  }
}

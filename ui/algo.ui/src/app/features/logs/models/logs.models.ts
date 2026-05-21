import { PaginatedResult } from '../../../core/models/paginated-result.model';

export interface ApplicationLogDto {
  readonly id: string | null;
  readonly timestamp: string;
  readonly level: string;
  readonly message: string;
  readonly messageTemplate: string | null;
  readonly exception: string | null;
  readonly properties: unknown;
  readonly traceId: string | null;
  readonly userId: string | null;
  readonly userName: string | null;
  readonly requestPath: string | null;
  readonly requestMethod: string | null;
  readonly statusCode: number | null;
  readonly elapsedMilliseconds: number | null;
}

export interface LogsQuery {
  readonly PageNumber: number;
  readonly PageSize: number;
  readonly Level?: string;
  readonly FromTimestamp?: string;
  readonly ToTimestamp?: string;
  readonly UserName?: string;
  readonly TraceId?: string;
  readonly RequestPath?: string;
  readonly RequestMethod?: string;
  readonly MessageContains?: string;
  readonly SortField?: string;
  readonly SortDirection?: 'Ascending' | 'Descending';
}

export type LogsPage = PaginatedResult<ApplicationLogDto>;

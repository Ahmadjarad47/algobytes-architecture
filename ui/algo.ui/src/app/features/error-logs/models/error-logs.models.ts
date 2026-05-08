import { PaginatedResult } from '../../../core/models/paginated-result.model';

export interface ErrorLogDto {
  readonly id: string | null;
  readonly timestamp: string;
  readonly level: string;
  readonly exceptionType: string;
  readonly message: string;
  readonly stackTrace: string | null;
  readonly source: string | null;
  readonly path: string | null;
  readonly method: string | null;
  readonly statusCode: number | null;
  readonly traceId: string | null;
  readonly userId: string | null;
  readonly userName: string | null;
  readonly requestBody: string | null;
  readonly queryString: string | null;
  readonly headers: unknown;
  readonly environment: string;
  readonly machineName: string;
  readonly createdAt: string;
}

export interface ErrorLogsQuery {
  readonly PageNumber: number;
  readonly PageSize: number;
  readonly ExceptionType?: string;
  readonly StatusCode?: number;
  readonly FromTimestamp?: string;
  readonly ToTimestamp?: string;
  readonly UserName?: string;
  readonly TraceId?: string;
  readonly Path?: string;
  readonly Method?: string;
  readonly MessageContains?: string;
  readonly SortField?: string;
  readonly SortDirection?: 'Ascending' | 'Descending';
}

export type ErrorLogsPage = PaginatedResult<ErrorLogDto>;

import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { TableLazyLoadEvent } from 'primeng/table';
import { finalize } from 'rxjs';

import { AdminDataTable } from '../../../../shared/components/admin-data-table/admin-data-table';
import { AdminDetailsDrawer } from '../../../../shared/components/admin-details-drawer/admin-details-drawer';
import {
  AdminDetailItem,
  AdminRowAction,
  AdminTableColumn
} from '../../../../shared/models/admin-table.model';
import { toTableQuery } from '../../../../shared/utils/admin-table.utils';
import { ErrorLogsApiService } from '../../api/error-logs-api.service';
import { ErrorLogDto } from '../../models/error-logs.models';
import { exportCsv, exportJson, ExportRow } from '../../../../shared/utils/export.utils';
import { AppToastService } from '../../../../core/services/app-toast.service';
import { Permissions } from '../../../../core/permissions/permission.catalog';
import { PermissionService } from '../../../../core/permissions/permission.service';

@Component({
  selector: 'app-error-logs-list',
  imports: [AdminDataTable, AdminDetailsDrawer],
  template: `
    <app-admin-data-table
      title="Error Logs"
      subtitle="Focused incident stream for exceptions and failed requests."
      [columns]="columns"
      [value]="logs()"
      [loading]="loading()"
      [lazy]="true"
      [rows]="pageSize()"
      [first]="first()"
      [totalRecords]="totalRecords()"
      [globalFilterFields]="['message', 'exceptionType', 'userName', 'traceId']"
      [showExport]="canExport()"
      searchPlaceholder="Search errors"
      emptyTitle="No error logs found"
      emptyMessage="Expand the date range or search for a different error signature."
      [showCreate]="false"
      [actions]="actions"
      (lazyLoad)="loadLogs($event)"
      (refresh)="reload()"
      (rowAction)="viewLog($event.row)"
      (exportCsv)="exportRows('error-logs', $event)"
      (exportJson)="exportRowsJson('error-logs', $event)"
    />

    <app-admin-details-drawer
      [visible]="detailsVisible()"
      [title]="selectedLog()?.exceptionType ?? 'Error details'"
      [items]="detailItems()"
      [showCopy]="true"
      actionLabel="Mark as resolved"
      actionIcon="pi pi-check"
      (visibleChange)="detailsVisible.set($event)"
      (copy)="copySelectedLog()"
      (action)="markResolvedPlaceholder()"
    />
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ErrorLogsList {
  private readonly api = inject(ErrorLogsApiService);
  private readonly toast = inject(AppToastService);
  private readonly permissionService = inject(PermissionService);
  protected readonly canExport = computed(() => this.permissionService.can({ any: [Permissions.errorLogs.read] }));

  protected readonly logs = signal<ErrorLogDto[]>([]);
  protected readonly loading = signal(false);
  protected readonly pageSize = signal(25);
  protected readonly first = signal(0);
  protected readonly totalRecords = signal(0);
  protected readonly detailsVisible = signal(false);
  protected readonly selectedLog = signal<ErrorLogDto | null>(null);

  protected readonly columns: AdminTableColumn[] = [
    {
      field: 'timestamp',
      header: 'Timestamp',
      sortable: true,
      cellType: 'date'
    },
    { field: 'exceptionType', header: 'Exception', filter: true },
    {
      field: 'level',
      header: 'Level',
      sortable: true,
      cellType: 'status',
      severityMap: {
        Error: 'danger',
        Warning: 'warn',
        Critical: 'contrast'
      }
    },
    { field: 'message', header: 'Message', filter: true },
    { field: 'method', header: 'Method', filter: true },
    { field: 'path', header: 'Path', filter: true },
    { field: 'statusCode', header: 'Status', filter: true, filterType: 'numeric' }
  ];

  protected readonly actions: AdminRowAction<ErrorLogDto>[] = [
    { id: 'view', label: 'View error details', icon: 'pi pi-eye' }
  ];

  protected readonly detailItems = computed<AdminDetailItem[]>(() => {
    const log = this.selectedLog();
    if (!log) {
      return [];
    }

    return [
      { label: 'Timestamp', value: log.timestamp, type: 'date' },
      {
        label: 'Level',
        value: log.level,
        type: 'status',
        severity: log.level === 'Error' ? 'danger' : 'warn'
      },
      { label: 'Exception type', value: log.exceptionType },
      { label: 'Message', value: log.message },
      { label: 'Stack trace', value: log.stackTrace, type: 'json' },
      { label: 'Source', value: log.source },
      { label: 'Path', value: log.path },
      { label: 'Method', value: log.method },
      { label: 'Status code', value: log.statusCode },
      { label: 'Trace ID', value: log.traceId },
      { label: 'Correlation ID', value: log.traceId },
      { label: 'User', value: log.userName },
      { label: 'Request body', value: log.requestBody, type: 'json' },
      { label: 'Payload placeholder', value: log.requestBody ?? 'No payload captured', type: 'json' },
      { label: 'Query string', value: log.queryString },
      { label: 'Headers', value: log.headers, type: 'json' },
      { label: 'Request headers placeholder', value: log.headers ?? {}, type: 'json' },
      { label: 'Environment', value: log.environment },
      { label: 'Machine', value: log.machineName },
      { label: 'Created at', value: log.createdAt, type: 'date' }
    ];
  });

  private lastLazyEvent: TableLazyLoadEvent = {
    first: 0,
    rows: 25
  };

  constructor() {
    this.loadLogs(this.lastLazyEvent);
  }

  protected loadLogs(event: TableLazyLoadEvent): void {
    this.lastLazyEvent = event;
    const query = toTableQuery(event, this.pageSize());

    this.loading.set(true);
    this.pageSize.set(query.pageSize);
    this.first.set((query.pageNumber - 1) * query.pageSize);

    this.api
      .getErrorLogs({
        PageNumber: query.pageNumber,
        PageSize: query.pageSize,
        MessageContains: query.search,
        SortField: query.sortField,
        SortDirection: query.sortDirection,
        ExceptionType: stringValue(query.filters?.['exceptionType']),
        Method: stringValue(query.filters?.['method']),
        Path: stringValue(query.filters?.['path']),
        StatusCode: numberValue(query.filters?.['statusCode'])
      })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe((response) => {
        this.logs.set(response.items);
        this.totalRecords.set(response.totalCount);
      });
  }

  protected reload(): void {
    this.loadLogs(this.lastLazyEvent);
  }

  protected viewLog(row: ErrorLogDto): void {
    if (!row.id) {
      this.selectedLog.set(row);
      this.detailsVisible.set(true);
      return;
    }

    this.api.getErrorLog(row.id).subscribe((log) => {
      this.selectedLog.set(log);
      this.detailsVisible.set(true);
    });
  }

  protected exportRows(fileName: string, rows: ErrorLogDto[]): void {
    exportCsv(fileName, rows as unknown as ExportRow[]);
  }

  protected exportRowsJson(fileName: string, rows: ErrorLogDto[]): void {
    exportJson(fileName, rows as unknown as ExportRow[]);
  }

  protected copySelectedLog(): void {
    void navigator.clipboard?.writeText(JSON.stringify(this.selectedLog(), null, 2));
    this.toast.success('Copied', 'Error details copied to clipboard.');
  }

  protected markResolvedPlaceholder(): void {
    this.toast.success('Marked as resolved', 'Placeholder action until an incident endpoint exists.');
  }
}

function stringValue(value: unknown): string | undefined {
  return typeof value === 'string' && value.trim().length > 0 ? value : undefined;
}

function numberValue(value: unknown): number | undefined {
  return typeof value === 'number' ? value : undefined;
}

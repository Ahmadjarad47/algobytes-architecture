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
import { LogsApiService } from '../../api/logs-api.service';
import { ApplicationLogDto } from '../../models/logs.models';
import { exportCsv, exportJson, ExportRow } from '../../../../shared/utils/export.utils';
import { AppToastService } from '../../../../core/services/app-toast.service';
import { Permissions } from '../../../../core/permissions/permission.catalog';
import { PermissionService } from '../../../../core/permissions/permission.service';

@Component({
  selector: 'app-logs-list',
  imports: [AdminDataTable, AdminDetailsDrawer],
  template: `
    <app-admin-data-table
      title="Application Logs"
      subtitle="Operational log stream with server-driven pagination and filtering."
      [columns]="columns"
      [value]="logs()"
      [loading]="loading()"
      [lazy]="true"
      [rows]="pageSize()"
      [first]="first()"
      [totalRecords]="totalRecords()"
      [globalFilterFields]="['message', 'userName', 'requestPath', 'traceId']"
      [showExport]="canExport()"
      searchPlaceholder="Search logs"
      emptyTitle="No logs found"
      emptyMessage="Adjust the query window or search terms to inspect traffic."
      [showCreate]="false"
      [actions]="actions"
      (lazyLoad)="loadLogs($event)"
      (refresh)="reload()"
      (rowAction)="viewLog($event.row)"
      (exportCsv)="exportRows('application-logs', $event)"
      (exportJson)="exportRowsJson('application-logs', $event)"
    />

    <app-admin-details-drawer
      [visible]="detailsVisible()"
      [title]="selectedLog()?.message ?? 'Log details'"
      [items]="detailItems()"
      [showCopy]="true"
      (visibleChange)="detailsVisible.set($event)"
      (copy)="copySelectedLog()"
    />
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class LogsList {
  private readonly api = inject(LogsApiService);
  private readonly toast = inject(AppToastService);
  private readonly permissionService = inject(PermissionService);
  protected readonly canExport = computed(() => this.permissionService.can({ any: [Permissions.logs.read] }));

  protected readonly logs = signal<ApplicationLogDto[]>([]);
  protected readonly loading = signal(false);
  protected readonly pageSize = signal(25);
  protected readonly first = signal(0);
  protected readonly totalRecords = signal(0);
  protected readonly detailsVisible = signal(false);
  protected readonly selectedLog = signal<ApplicationLogDto | null>(null);

  protected readonly columns: AdminTableColumn[] = [
    {
      field: 'timestamp',
      header: 'Timestamp',
      sortable: true,
      cellType: 'date'
    },
    {
      field: 'level',
      header: 'Level',
      sortable: true,
      filter: true,
      cellType: 'status',
      severityMap: {
        Information: 'info',
        Warning: 'warn',
        Error: 'danger',
        Debug: 'secondary'
      }
    },
    { field: 'message', header: 'Message', filter: true },
    { field: 'userName', header: 'User', filter: true },
    { field: 'requestMethod', header: 'Method', filter: true },
    { field: 'requestPath', header: 'Path', filter: true },
    { field: 'statusCode', header: 'Status', sortable: true, filter: true, filterType: 'numeric' }
  ];

  protected readonly actions: AdminRowAction<ApplicationLogDto>[] = [
    { id: 'view', label: 'View log details', icon: 'pi pi-eye' }
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
        severity: log.level === 'Error' ? 'danger' : log.level === 'Warning' ? 'warn' : 'info'
      },
      { label: 'Message', value: log.message },
      { label: 'Source', value: log.requestPath },
      { label: 'Environment', value: 'Current workspace' },
      { label: 'Message template', value: log.messageTemplate },
      { label: 'Exception', value: log.exception, type: 'json' },
      { label: 'Properties', value: log.properties, type: 'json' },
      { label: 'Trace ID', value: log.traceId },
      { label: 'Request ID', value: log.traceId },
      { label: 'User', value: log.userName },
      { label: 'Path', value: log.requestPath },
      { label: 'Method', value: log.requestMethod },
      { label: 'Status code', value: log.statusCode },
      { label: 'Elapsed (ms)', value: log.elapsedMilliseconds }
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
      .getLogs({
        PageNumber: query.pageNumber,
        PageSize: query.pageSize,
        MessageContains: query.search,
        SortField: query.sortField,
        SortDirection: query.sortDirection,
        Level: stringValue(query.filters?.['level']),
        UserName: stringValue(query.filters?.['userName']),
        RequestMethod: stringValue(query.filters?.['requestMethod']),
        RequestPath: stringValue(query.filters?.['requestPath'])
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

  protected viewLog(row: ApplicationLogDto): void {
    if (!row.id) {
      this.selectedLog.set(row);
      this.detailsVisible.set(true);
      return;
    }

    this.api.getLog(row.id).subscribe((log) => {
      this.selectedLog.set(log);
      this.detailsVisible.set(true);
    });
  }

  protected exportRows(fileName: string, rows: ApplicationLogDto[]): void {
    exportCsv(fileName, rows as unknown as ExportRow[]);
  }

  protected exportRowsJson(fileName: string, rows: ApplicationLogDto[]): void {
    exportJson(fileName, rows as unknown as ExportRow[]);
  }

  protected copySelectedLog(): void {
    void navigator.clipboard?.writeText(JSON.stringify(this.selectedLog(), null, 2));
    this.toast.success('Copied', 'Log details copied to clipboard.');
  }
}

function stringValue(value: unknown): string | undefined {
  return typeof value === 'string' && value.trim().length > 0 ? value : undefined;
}

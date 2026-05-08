import {
  ChangeDetectionStrategy,
  Component,
  ViewChild,
  input,
  output
} from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { InputTextModule } from 'primeng/inputtext';
import { SkeletonModule } from 'primeng/skeleton';
import { Table, TableLazyLoadEvent, TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { ToolbarModule } from 'primeng/toolbar';
import { TooltipModule } from 'primeng/tooltip';

import { AdminRowAction, AdminTableColumn } from '../../models/admin-table.model';

@Component({
  selector: 'app-admin-data-table',
  imports: [
    CommonModule,
    FormsModule,
    ButtonModule,
    IconFieldModule,
    InputIconModule,
    InputTextModule,
    SkeletonModule,
    TableModule,
    TagModule,
    ToolbarModule,
    TooltipModule,
    DatePipe
  ],
  template: `
    <section class="surface-card overflow-hidden rounded-xl">
      <p-toolbar styleClass="rounded-none border-b border-surface-200 px-3 py-2.5">
        <ng-template #start>
          <div class="flex flex-col gap-1">
            <h2 class="m-0 text-base font-semibold leading-tight text-surface-950">{{ title() }}</h2>
            @if (subtitle()) {
              <p class="m-0 text-xs text-surface-500">{{ subtitle() }}</p>
            }
          </div>
        </ng-template>

        <ng-template #end>
          <div class="flex w-full flex-col gap-2 md:w-auto md:flex-row md:items-center">
            <p-iconfield>
              <p-inputicon class="pi pi-search" />
              <input
                pInputText
                [(ngModel)]="searchValue"
                [placeholder]="searchPlaceholder()"
                class="w-full md:w-64"
                (input)="applyGlobalFilter()"
              />
            </p-iconfield>

            <div class="flex items-center gap-2">
              <p-button
                icon="pi pi-refresh"
                label="Refresh"
                severity="secondary"
                size="small"
                [outlined]="true"
                (onClick)="refresh.emit()"
              />

              @if (showCreate()) {
                <p-button
                  icon="pi pi-plus"
                  [label]="createLabel()"
                  size="small"
                  (onClick)="create.emit()"
                />
              }
            </div>
          </div>
        </ng-template>
      </p-toolbar>

      <p-table
        #table
        [value]="loading() ? skeletonRows() : value()"
        [columns]="columns()"
        [dataKey]="dataKey()"
        [paginator]="true"
        [rows]="rows()"
        [first]="first()"
        [totalRecords]="totalRecords()"
        [rowsPerPageOptions]="rowsPerPageOptions()"
        paginatorDropdownAppendTo="body"
        paginatorDropdownScrollHeight="180px"
        [lazy]="lazy()"
        [globalFilterFields]="globalFilterFields()"
        responsiveLayout="stack"
        breakpoint="960px"
        sortMode="single"
        [showCurrentPageReport]="true"
        currentPageReportTemplate="Showing {first} to {last} of {totalRecords}"
        styleClass="p-datatable-sm p-datatable-gridlines"
        tableStyleClass="min-w-full"
        (onLazyLoad)="lazyLoad.emit($event)"
      >
        <ng-template #header let-columns>
          <tr>
            @for (column of columns; track column.field) {
              <th
                [pSortableColumn]="column.sortable ? column.field : undefined"
                [class]="column.widthClass ?? ''"
              >
                <div class="flex items-center justify-between gap-2">
                  <span>{{ column.header }}</span>
                  @if (column.sortable) {
                    <p-sortIcon [field]="column.field" />
                  }
                </div>
                @if (column.filter) {
                  <p-columnFilter
                    [field]="column.field"
                    [type]="column.filterType ?? 'text'"
                    display="menu"
                    [showOperator]="false"
                    [showAddButton]="false"
                    [showMatchModes]="false"
                    [placeholder]="column.placeholder ?? ('Filter ' + column.header)"
                  />
                }
              </th>
            }
            @if (actions().length > 0) {
              <th class="w-32 text-right">Actions</th>
            }
          </tr>
        </ng-template>

        <ng-template #body let-rowData let-columns="columns">
          <tr>
            @if (loading()) {
              @for (column of columns; track column.field) {
                <td>
                  <span class="mb-2 block text-xs font-medium uppercase tracking-wide text-surface-400 md:hidden">
                    {{ column.header }}
                  </span>
                  <p-skeleton [width]="skeletonWidth($index)" height="1rem" borderRadius="6px" />
                </td>
              }

              @if (actions().length > 0) {
                <td>
                  <div class="flex items-center justify-end gap-1.5">
                    @for (action of actions(); track action.id) {
                      <p-skeleton shape="circle" size="1.75rem" />
                    }
                  </div>
                </td>
              }
            } @else {
              @for (column of columns; track column.field) {
                <td>
                  <span class="mb-2 block text-xs font-medium uppercase tracking-wide text-surface-400 md:hidden">
                    {{ column.header }}
                  </span>

                  @switch (column.cellType ?? 'text') {
                    @case ('date') {
                      {{ asDateValue(read(rowData, column.field)) | date: 'medium' }}
                    }
                    @case ('boolean') {
                      <p-tag
                        [value]="read(rowData, column.field) ? 'Yes' : 'No'"
                        [severity]="read(rowData, column.field) ? 'success' : 'secondary'"
                      />
                    }
                    @case ('status') {
                      <p-tag
                        [value]="formatValue(read(rowData, column.field))"
                        [severity]="resolveSeverity(column, read(rowData, column.field))"
                      />
                    }
                    @case ('json') {
                      <span class="line-clamp-2 text-sm text-surface-600">
                        {{ formatJson(read(rowData, column.field)) }}
                      </span>
                    }
                    @case ('list') {
                      <span>{{ formatList(read(rowData, column.field)) }}</span>
                    }
                    @default {
                      <span>{{ formatValue(read(rowData, column.field)) }}</span>
                    }
                  }
                </td>
              }

              @if (actions().length > 0) {
                <td>
                  <div class="flex items-center justify-end gap-1.5">
                    @for (action of actions(); track action.id) {
                      <p-button
                        [icon]="action.icon"
                        [severity]="action.severity ?? 'secondary'"
                        size="small"
                        [outlined]="true"
                        [rounded]="true"
                        [text]="true"
                        [disabled]="action.disabled?.(rowData) ?? false"
                        [pTooltip]="action.label"
                        tooltipPosition="top"
                        (onClick)="rowAction.emit({ actionId: action.id, row: rowData })"
                      />
                    }
                  </div>
                </td>
              }
            }
          </tr>
        </ng-template>

        <ng-template #emptymessage>
          <tr>
            <td [attr.colspan]="columns().length + (actions().length > 0 ? 1 : 0)">
              <div class="flex flex-col items-center gap-2 px-6 py-10 text-center">
                <i class="pi pi-inbox text-2xl text-surface-300"></i>
                <h3 class="m-0 text-sm font-semibold text-surface-700">{{ emptyTitle() }}</h3>
                <p class="m-0 max-w-md text-xs text-surface-500">{{ emptyMessage() }}</p>
              </div>
            </td>
          </tr>
        </ng-template>
      </p-table>
    </section>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AdminDataTable {
  @ViewChild('table') private readonly table?: Table;

  readonly title = input.required<string>();
  readonly subtitle = input('');
  readonly columns = input<AdminTableColumn[]>([]);
  readonly value = input<any[]>([]);
  readonly dataKey = input('id');
  readonly loading = input(false);
  readonly lazy = input(false);
  readonly rows = input(25);
  readonly first = input(0);
  readonly totalRecords = input(0);
  readonly rowsPerPageOptions = input<number[]>([25, 50, 100]);
  readonly globalFilterFields = input<string[]>([]);
  readonly searchPlaceholder = input('Search');
  readonly emptyTitle = input('No records found');
  readonly emptyMessage = input('Try refining your filters or add a new record.');
  readonly showCreate = input(true);
  readonly createLabel = input('Create');
  readonly actions = input<AdminRowAction<any>[]>([]);

  readonly create = output<void>();
  readonly refresh = output<void>();
  readonly lazyLoad = output<TableLazyLoadEvent>();
  readonly rowAction = output<{ actionId: string; row: any }>();

  searchValue = '';

  skeletonRows(): Record<string, number>[] {
    return Array.from({ length: Math.min(this.rows(), 10) }, (_, index) => ({ id: index }));
  }

  skeletonWidth(index: number): string {
    const widths = ['68%', '86%', '58%', '42%', '50%', '74%'];

    return widths[index % widths.length];
  }

  applyGlobalFilter(): void {
    this.table?.filterGlobal(this.searchValue, 'contains');
  }

  read(row: Record<string, unknown>, field: string): unknown {
    return row[field];
  }

  asDateValue(value: unknown): string | number | Date | null | undefined {
    if (
      value === null ||
      value === undefined ||
      typeof value === 'string' ||
      typeof value === 'number' ||
      value instanceof Date
    ) {
      return value;
    }

    return undefined;
  }

  formatValue(value: unknown): string {
    if (value === null || value === undefined || value === '') {
      return '-';
    }

    return String(value);
  }

  formatList(value: unknown): string {
    if (!Array.isArray(value) || value.length === 0) {
      return '-';
    }

    return value.join(', ');
  }

  formatJson(value: unknown): string {
    if (value === null || value === undefined || value === '') {
      return '-';
    }

    if (typeof value === 'string') {
      return value;
    }

    return JSON.stringify(value);
  }

  resolveSeverity(
    column: AdminTableColumn,
    value: unknown
  ): 'success' | 'info' | 'warn' | 'danger' | 'secondary' | 'contrast' {
    const key = String(value ?? '');

    return column.severityMap?.[key] ?? 'secondary';
  }
}

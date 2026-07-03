import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize } from 'rxjs';

import { AdminDataTable } from '../../../../shared/components/admin-data-table/admin-data-table';
import { AdminDetailsDrawer } from '../../../../shared/components/admin-details-drawer/admin-details-drawer';
import { AdminFormDialog } from '../../../../shared/components/admin-form-dialog/admin-form-dialog';
import {
  AdminDetailItem,
  AdminFormField,
  AdminRowAction,
  AdminTableColumn
} from '../../../../shared/models/admin-table.model';
import { AppToastService } from '../../../../core/services/app-toast.service';
import { AdminActionBusService } from '../../../../core/services/admin-action-bus.service';
import { Permissions } from '../../../../core/permissions/permission.catalog';
import { PermissionService } from '../../../../core/permissions/permission.service';
import { exportCsv, exportJson, ExportRow } from '../../../../shared/utils/export.utils';
import { OrdersApiService } from '../../api/orders-api.service';
import { CreateOrderCommand, CreateOrderItemModel, OrderDto } from '../../models/orders.models';

const PAYMENT_METHOD_OPTIONS = ['Wallet', 'CreditCard', 'PayPal'] as const;

@Component({
  selector: 'app-orders-list',
  imports: [
    ReactiveFormsModule,
    AdminDataTable,
    AdminFormDialog,
    AdminDetailsDrawer
  ],
  template: `
    <app-admin-data-table
      title="Orders"
      subtitle="Manage shop orders with line items, status tracking, and dynamic custom fields."
      [columns]="columns"
      [value]="orders()"
      [loading]="loading()"
      [lazy]="false"
      [rows]="25"
      [totalRecords]="orders().length"
      [globalFilterFields]="globalFilterFields"
      [showCreate]="canCreate()"
      [showExport]="canExport()"
      searchPlaceholder="Search orders"
      emptyTitle="No orders found"
      emptyMessage="Create an order to start recording purchases."
      [actions]="actions()"
      (refresh)="loadOrders()"
      (create)="openCreate()"
      (rowAction)="handleAction($event.actionId, $event.row)"
      (exportCsv)="exportRows('orders', $event)"
      (exportJson)="exportRowsJson('orders', $event)"
    />

    <app-admin-form-dialog
      [visible]="formVisible()"
      title="Create order"
      [form]="form"
      [fields]="fields"
      submitLabel="Create order"
      [loading]="saving()"
      (visibleChange)="closeForm($event)"
      (submit)="save()"
    />

    <app-admin-details-drawer
      [visible]="detailsVisible()"
      [title]="selectedOrder()?.orderNumber ?? 'Order details'"
      [items]="detailItems()"
      (visibleChange)="detailsVisible.set($event)"
    />
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class OrdersList {
  private readonly api = inject(OrdersApiService);
  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly toast = inject(AppToastService);
  private readonly actionBus = inject(AdminActionBusService);
  private readonly permissionService = inject(PermissionService);

  protected readonly orders = signal<OrderDto[]>([]);
  protected readonly loading = signal(false);
  protected readonly saving = signal(false);
  protected readonly formVisible = signal(false);
  protected readonly detailsVisible = signal(false);
  protected readonly selectedOrder = signal<OrderDto | null>(null);

  protected readonly globalFilterFields = ['orderNumber', 'userId', 'currencyCode', 'paymentMethod', 'orderStatus'];
  protected readonly columns: AdminTableColumn[] = [
    { field: 'id', header: 'ID', sortable: true },
    { field: 'orderNumber', header: 'Order number', sortable: true, filter: true },
    { field: 'userId', header: 'User ID', filter: true },
    { field: 'currencyCode', header: 'Currency', sortable: true, filter: true },
    { field: 'totalAmount', header: 'Total amount', sortable: true, cellType: 'currency', currencyCode: 'USD' },
    { field: 'paymentMethod', header: 'Payment method', filter: true },
    {
      field: 'orderStatus',
      header: 'Status',
      cellType: 'status',
      sortable: true,
      filter: true,
      severityMap: {
        Pending: 'warn',
        Processing: 'info',
        Paid: 'success',
        Failed: 'danger'
      }
    },
    { field: 'createdAt', header: 'Created', cellType: 'date', sortable: true }
  ];

  protected readonly fields: AdminFormField[] = [
    { key: 'orderNumber', label: 'Order number', type: 'text', required: true },
    {
      key: 'paymentMethod',
      label: 'Payment method',
      type: 'select',
      options: PAYMENT_METHOD_OPTIONS.map((value) => ({ label: value, value }))
    },
    { key: 'exchangeRateUsedToBase', label: 'Exchange rate to base', type: 'number' },
    {
      key: 'itemsJson',
      label: 'Order items JSON',
      type: 'json',
      required: true,
      placeholder: '[{"productId":1,"quantity":1}]'
    },
    {
      key: 'customFieldsJson',
      label: 'Custom fields JSON',
      type: 'json',
      placeholder: '{"campaign":"summer-sale"}'
    }
  ];

  protected readonly canCreate = computed(() =>
    this.permissionService.can({ any: [Permissions.orders.create] })
  );
  protected readonly canExport = computed(() =>
    this.permissionService.can({ any: [Permissions.orders.read] })
  );

  protected readonly actions = computed<AdminRowAction<OrderDto>[]>(() => [
    { id: 'view', label: 'View order', icon: 'pi pi-eye' }
  ]);

  protected readonly form = this.formBuilder.group({
    orderNumber: ['', Validators.required],
    paymentMethod: ['Wallet'],
    exchangeRateUsedToBase: [null as number | null],
    itemsJson: ['[\n  {\n    "productId": 1,\n    "quantity": 1\n  }\n]', Validators.required],
    customFieldsJson: ['']
  });

  protected readonly detailItems = computed<AdminDetailItem[]>(() => {
    const order = this.selectedOrder();
    if (!order) {
      return [];
    }

    return [
      { label: 'Order ID', value: order.id },
      { label: 'User ID', value: order.userId },
      { label: 'Order number', value: order.orderNumber },
      { label: 'Currency', value: order.currencyCode },
      { label: 'Total amount', value: order.totalAmount },
      { label: 'Exchange rate', value: order.exchangeRateUsedToBase },
      { label: 'Payment method', value: order.paymentMethod },
      {
        label: 'Status',
        value: order.orderStatus,
        type: 'status',
        severity: mapStatusSeverity(order.orderStatus)
      },
      { label: 'Created at', value: order.createdAt, type: 'date' },
      { label: 'Custom fields', value: order.customFields, type: 'json' },
      { label: 'Items', value: order.items, type: 'json' },
      { label: 'Payments', value: order.payments, type: 'json' }
    ];
  });

  constructor() {
    this.loadOrders();
    this.actionBus.actions$.subscribe((action) => {
      if (action === 'create-order' && this.canCreate()) {
        this.openCreate();
      }
    });
  }

  protected loadOrders(): void {
    this.loading.set(true);
    this.api
      .getOrders()
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe((orders) => this.orders.set(orders));
  }

  protected openCreate(): void {
    this.form.reset({
      orderNumber: '',
      paymentMethod: 'Wallet',
      exchangeRateUsedToBase: null,
      itemsJson: '[\n  {\n    "productId": 1,\n    "quantity": 1\n  }\n]',
      customFieldsJson: ''
    });
    this.formVisible.set(true);
  }

  protected closeForm(visible: boolean): void {
    this.formVisible.set(visible);
  }

  protected handleAction(actionId: string, row: OrderDto): void {
    if (actionId !== 'view') {
      return;
    }

    this.api.getOrder(row.id).subscribe((order) => {
      this.selectedOrder.set(order);
      this.detailsVisible.set(true);
    });
  }

  protected save(): void {
    if (this.form.invalid || this.saving()) {
      return;
    }

    const value = this.form.getRawValue();
    const items = parseJsonArray<CreateOrderItemModel>(value.itemsJson);
    if (!items || !items.length) {
      this.toast.error('Invalid order items', 'Provide at least one order item in valid JSON format.');
      return;
    }

    const customFields = parseJsonObject(value.customFieldsJson);
    if (value.customFieldsJson.trim() && customFields === null) {
      this.toast.error('Invalid custom fields', 'Custom fields must be a valid JSON object.');
      return;
    }

    const payload: CreateOrderCommand = {
      orderNumber: value.orderNumber,
      paymentMethod: value.paymentMethod || null,
      exchangeRateUsedToBase: value.exchangeRateUsedToBase,
      items,
      customFields
    };

    this.saving.set(true);
    this.api
      .createOrder(payload)
      .pipe(finalize(() => this.saving.set(false)))
      .subscribe(() => {
        this.toast.success('Order created', payload.orderNumber);
        this.formVisible.set(false);
        this.loadOrders();
      });
  }

  protected exportRows(fileName: string, rows: OrderDto[]): void {
    exportCsv(fileName, rows as unknown as ExportRow[]);
  }

  protected exportRowsJson(fileName: string, rows: OrderDto[]): void {
    exportJson(fileName, rows as unknown as ExportRow[]);
  }
}

function parseJsonArray<T>(value: string): T[] | null {
  try {
    const parsed = JSON.parse(value || '[]') as unknown;
    if (!Array.isArray(parsed)) {
      return null;
    }

    return parsed as T[];
  } catch {
    return null;
  }
}

function parseJsonObject(value: string): Record<string, unknown> | null {
  if (!value.trim()) {
    return null;
  }

  try {
    const parsed = JSON.parse(value) as unknown;
    if (!parsed || typeof parsed !== 'object' || Array.isArray(parsed)) {
      return null;
    }

    return parsed as Record<string, unknown>;
  } catch {
    return null;
  }
}

function mapStatusSeverity(status: string): AdminDetailItem['severity'] {
  switch (status) {
    case 'Paid':
      return 'success';
    case 'Failed':
      return 'danger';
    case 'Processing':
      return 'info';
    default:
      return 'warn';
  }
}

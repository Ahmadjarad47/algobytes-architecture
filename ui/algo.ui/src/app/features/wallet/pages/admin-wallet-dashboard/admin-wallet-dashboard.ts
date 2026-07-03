import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { finalize } from 'rxjs';
import { ButtonModule } from 'primeng/button';
import { ChartModule } from 'primeng/chart';
import type { ChartData, ChartOptions } from 'chart.js';

import { Permissions } from '../../../../core/permissions/permission.catalog';
import { PermissionService } from '../../../../core/permissions/permission.service';
import { AppToastService } from '../../../../core/services/app-toast.service';
import { AdminConfirmDialog } from '../../../../shared/components/admin-confirm-dialog/admin-confirm-dialog';
import { AdminDataTable } from '../../../../shared/components/admin-data-table/admin-data-table';
import { AdminRowAction, AdminTableColumn } from '../../../../shared/models/admin-table.model';
import { exportCsv, exportJson, ExportRow } from '../../../../shared/utils/export.utils';
import { WalletApiService } from '../../api/wallet-api.service';
import {
  AdminWalletOverviewDto,
  AdminWalletTransactionDto,
  AdminWalletUserDto
} from '../../models/wallet.models';

interface PrimeChartConfig {
  readonly data: ChartData;
  readonly options: ChartOptions;
}

@Component({
  selector: 'app-admin-wallet-dashboard',
  imports: [
    DecimalPipe,
    ButtonModule,
    ChartModule,
    AdminDataTable,
    AdminConfirmDialog
  ],
  template: `
    <section class="surface-card dashboard-section mb-3 overflow-hidden">
      <div class="flex flex-col gap-3 lg:flex-row lg:items-start lg:justify-between">
        <div>
          <div class="text-[11px] font-semibold uppercase tracking-wide text-slate-500">Admin wallet control</div>
          <h2 class="m-0 mt-1 text-sm font-semibold text-slate-950">All wallet balances and ledger movement</h2>
          <p class="m-0 mt-1 max-w-3xl text-[12px] text-slate-500">
            Monitor every user wallet, deposit and withdrawal totals, frozen funds, and risky balances from one admin-only screen.
          </p>
        </div>

        <p-button
          label="Refresh"
          icon="pi pi-refresh"
          size="small"
          severity="secondary"
          [outlined]="true"
          [loading]="loading()"
          (onClick)="loadOverview()"
        />
      </div>

      <div class="mt-3 grid gap-2 md:grid-cols-2 xl:grid-cols-4">
        <article class="rounded-xl border border-slate-200 bg-slate-50 px-3 py-2">
          <div class="text-[11px] text-slate-500">Wallets</div>
          <div class="mt-1 text-lg font-semibold text-slate-900">{{ wallets().length }}</div>
        </article>
        <article class="rounded-xl border border-slate-200 bg-slate-50 px-3 py-2">
          <div class="text-[11px] text-slate-500">Transactions</div>
          <div class="mt-1 text-lg font-semibold text-slate-900">{{ transactions().length }}</div>
        </article>
        <article class="rounded-xl border border-emerald-200 bg-emerald-50 px-3 py-2">
          <div class="text-[11px] text-emerald-700">Total deposits</div>
          <div class="mt-1 text-lg font-semibold text-emerald-950">{{ totalDeposits() | number: '1.2-2' }}</div>
        </article>
        <article class="rounded-xl border border-rose-200 bg-rose-50 px-3 py-2">
          <div class="text-[11px] text-rose-700">Total withdrawals</div>
          <div class="mt-1 text-lg font-semibold text-rose-950">{{ totalWithdrawals() | number: '1.2-2' }}</div>
        </article>
      </div>
    </section>

    <section class="mb-3 grid gap-3 xl:grid-cols-[minmax(0,1fr)_minmax(0,0.8fr)]">
      <article class="surface-card dashboard-section">
        <div class="mb-2">
          <div class="text-[11px] font-semibold uppercase tracking-wide text-slate-500">Portfolio balance</div>
          <div class="mt-1 text-sm font-semibold text-slate-950">Balance by currency</div>
        </div>
        <p-chart
          type="bar"
          styleClass="dashboard-prime-chart block"
          height="240px"
          [data]="balanceChart().data"
          [options]="balanceChart().options"
        />
      </article>

      <article class="surface-card dashboard-section">
        <div class="mb-2">
          <div class="text-[11px] font-semibold uppercase tracking-wide text-slate-500">Money flow</div>
          <div class="mt-1 text-sm font-semibold text-slate-950">Deposits vs withdrawals</div>
        </div>
        <p-chart
          type="doughnut"
          styleClass="dashboard-prime-chart block"
          height="240px"
          [data]="flowChart().data"
          [options]="flowChart().options"
        />
      </article>

      <article class="surface-card dashboard-section xl:col-span-2">
        <div class="mb-2">
          <div class="text-[11px] font-semibold uppercase tracking-wide text-slate-500">Daily ledger</div>
          <div class="mt-1 text-sm font-semibold text-slate-950">Net movement over time</div>
        </div>
        <p-chart
          type="line"
          styleClass="dashboard-prime-chart block"
          height="240px"
          [data]="dailyMovementChart().data"
          [options]="dailyMovementChart().options"
        />
      </article>
    </section>

    <div class="mb-3">
      <app-admin-data-table
        title="All wallets"
        subtitle="Admin-only wallet balances by user. Stop freezes every positive balance; delete removes the user's wallet ledger."
        [columns]="walletColumns"
        [value]="wallets()"
        [loading]="loading()"
        [lazy]="false"
        [rows]="15"
        [totalRecords]="wallets().length"
        [globalFilterFields]="walletGlobalFilterFields"
        [showCreate]="false"
        [showExport]="canRead()"
        [actions]="walletActions()"
        searchPlaceholder="Search wallets"
        emptyTitle="No wallets"
        emptyMessage="Wallet activity will appear after users deposit or spend funds."
        (refresh)="loadOverview()"
        (rowAction)="handleWalletAction($event.actionId, $event.row)"
        (exportCsv)="exportRows('admin-wallets', $event)"
        (exportJson)="exportRowsJson('admin-wallets', $event)"
      />
    </div>

    <app-admin-data-table
      title="All wallet transactions"
      subtitle="Full admin ledger across every user, currency, deposit, withdrawal, freeze, refund, and purchase."
      [columns]="transactionColumns"
      [value]="transactions()"
      [loading]="loading()"
      [lazy]="false"
      [rows]="25"
      [totalRecords]="transactions().length"
      [globalFilterFields]="transactionGlobalFilterFields"
      [showCreate]="false"
      [showExport]="canRead()"
      searchPlaceholder="Search transactions"
      emptyTitle="No wallet transactions"
      emptyMessage="No wallet ledger entries have been created yet."
      (refresh)="loadOverview()"
      (exportCsv)="exportRows('admin-wallet-transactions', $event)"
      (exportJson)="exportRowsJson('admin-wallet-transactions', $event)"
    />

    <app-admin-confirm-dialog
      [visible]="stopDialogVisible()"
      title="Stop wallet"
      [message]="'Stop wallet for ' + (pendingWallet()?.email ?? pendingWallet()?.displayName ?? 'this user') + '?'"
      description="This freezes every positive balance for the user by creating admin freeze transactions."
      confirmLabel="Stop wallet"
      [loading]="stopping()"
      (visibleChange)="closeStopDialog($event)"
      (confirm)="confirmStopWallet()"
    />

    <app-admin-confirm-dialog
      [visible]="deleteDialogVisible()"
      title="Delete wallet transactions"
      [message]="'Delete all wallet transactions for ' + (pendingWallet()?.email ?? pendingWallet()?.displayName ?? 'this user') + '?'"
      description="This removes the user's wallet ledger entries and resets their wallet history."
      confirmLabel="Delete ledger"
      [loading]="deleting()"
      (visibleChange)="closeDeleteDialog($event)"
      (confirm)="confirmDeleteTransactions()"
    />
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AdminWalletDashboard {
  private readonly api = inject(WalletApiService);
  private readonly toast = inject(AppToastService);
  private readonly permissionService = inject(PermissionService);

  protected readonly loading = signal(false);
  protected readonly stopping = signal(false);
  protected readonly deleting = signal(false);
  protected readonly overview = signal<AdminWalletOverviewDto | null>(null);
  protected readonly pendingWallet = signal<AdminWalletUserDto | null>(null);
  protected readonly stopDialogVisible = signal(false);
  protected readonly deleteDialogVisible = signal(false);

  protected readonly canRead = computed(() =>
    this.permissionService.can({ any: [Permissions.wallet.read] })
  );
  protected readonly canUpdate = computed(() =>
    this.permissionService.can({ any: [Permissions.wallet.update] })
  );
  protected readonly canDelete = computed(() =>
    this.permissionService.can({ any: [Permissions.wallet.delete] })
  );

  protected readonly wallets = computed(() => this.overview()?.wallets ?? []);
  protected readonly transactions = computed(() => this.overview()?.transactions ?? []);
  protected readonly currencySummaries = computed(() => this.overview()?.currencySummaries ?? []);
  protected readonly dailyMovements = computed(() => this.overview()?.dailyMovements ?? []);
  protected readonly totalDeposits = computed(() =>
    this.currencySummaries().reduce((sum, item) => sum + item.totalDeposits, 0)
  );
  protected readonly totalWithdrawals = computed(() =>
    this.currencySummaries().reduce((sum, item) => sum + item.totalWithdrawals, 0)
  );

  protected readonly walletGlobalFilterFields = ['email', 'userName', 'displayName'];
  protected readonly transactionGlobalFilterFields = ['email', 'displayName', 'currencyCode', 'transactionType', 'description', 'referenceId'];

  protected readonly walletColumns: AdminTableColumn[] = [
    { field: 'email', header: 'Email', sortable: true, filter: true },
    { field: 'displayName', header: 'Name', sortable: true, filter: true },
    { field: 'isActive', header: 'Active', sortable: true, cellType: 'boolean' },
    { field: 'totalBalance', header: 'Balance', sortable: true, cellType: 'currency', currencyCode: 'USD' },
    { field: 'totalDeposits', header: 'Deposits', sortable: true, cellType: 'currency', currencyCode: 'USD' },
    { field: 'totalWithdrawals', header: 'Withdrawals', sortable: true, cellType: 'currency', currencyCode: 'USD' },
    { field: 'totalFrozen', header: 'Frozen', sortable: true, cellType: 'currency', currencyCode: 'USD' },
    { field: 'transactionCount', header: 'Transactions', sortable: true },
    { field: 'lastTransactionAt', header: 'Last activity', sortable: true, cellType: 'date' }
  ];

  protected readonly transactionColumns: AdminTableColumn[] = [
    { field: 'id', header: 'ID', sortable: true },
    { field: 'email', header: 'User', sortable: true, filter: true },
    { field: 'currencyCode', header: 'Currency', sortable: true, filter: true },
    { field: 'amount', header: 'Amount', sortable: true, cellType: 'currency', currencyCode: 'USD' },
    { field: 'transactionType', header: 'Type', sortable: true, filter: true },
    { field: 'description', header: 'Description', filter: true },
    { field: 'referenceId', header: 'Reference', filter: true },
    { field: 'createdAt', header: 'Created', sortable: true, cellType: 'date' }
  ];

  protected readonly walletActions = computed<AdminRowAction<AdminWalletUserDto>[]>(() => [
    ...(this.canUpdate()
      ? [{ id: 'stop', label: 'Stop wallet', icon: 'pi pi-lock', severity: 'warn' as const }]
      : []),
    ...(this.canDelete()
      ? [{ id: 'delete-transactions', label: 'Delete transactions', icon: 'pi pi-trash', severity: 'danger' as const }]
      : [])
  ]);

  protected readonly balanceChart = computed<PrimeChartConfig>(() => ({
    data: {
      labels: this.currencySummaries().map((item) => item.currencyCode),
      datasets: [
        {
          label: 'Balance',
          data: this.currencySummaries().map((item) => item.totalBalance),
          backgroundColor: '#14b8a6',
          borderRadius: 8,
          borderSkipped: false
        },
        {
          label: 'Frozen',
          data: this.currencySummaries().map((item) => item.totalFrozen),
          backgroundColor: '#f59e0b',
          borderRadius: 8,
          borderSkipped: false
        }
      ]
    },
    options: this.cartesianOptions()
  }));

  protected readonly flowChart = computed<PrimeChartConfig>(() => ({
    data: {
      labels: ['Deposits', 'Withdrawals', 'Frozen'],
      datasets: [
        {
          data: [
            this.totalDeposits(),
            this.totalWithdrawals(),
            this.currencySummaries().reduce((sum, item) => sum + item.totalFrozen, 0)
          ],
          backgroundColor: ['#14b8a6', '#ef4444', '#f59e0b'],
          borderColor: '#ffffff',
          borderWidth: 3,
          hoverOffset: 8
        }
      ]
    },
    options: {
      responsive: true,
      maintainAspectRatio: false,
      cutout: '66%',
      plugins: {
        legend: {
          display: true,
          position: 'bottom',
          labels: { color: '#64748b', boxWidth: 10, boxHeight: 10, font: { size: 11 } }
        }
      }
    }
  }));

  protected readonly dailyMovementChart = computed<PrimeChartConfig>(() => {
    const labels = Array.from(new Set(this.dailyMovements().map((item) => item.date))).slice(-21);
    const currencies = Array.from(new Set(this.dailyMovements().map((item) => item.currencyCode)));
    const colors = ['#14b8a6', '#6366f1', '#f59e0b', '#0f172a'];

    return {
      data: {
        labels,
        datasets: currencies.map((currencyCode, index) => ({
          label: currencyCode,
          data: labels.map((date) =>
            this.dailyMovements()
              .filter((item) => item.date === date && item.currencyCode === currencyCode)
              .reduce((sum, item) => sum + item.netMovement, 0)
          ),
          borderColor: colors[index % colors.length],
          backgroundColor: `${colors[index % colors.length]}22`,
          tension: 0.35,
          fill: true
        }))
      },
      options: this.cartesianOptions()
    };
  });

  constructor() {
    this.loadOverview();
  }

  protected loadOverview(): void {
    this.loading.set(true);
    this.api
      .getAdminOverview()
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe((overview) => this.overview.set(overview));
  }

  protected handleWalletAction(actionId: string, row: AdminWalletUserDto): void {
    this.pendingWallet.set(row);

    if (actionId === 'stop') {
      this.stopDialogVisible.set(true);
      return;
    }

    if (actionId === 'delete-transactions') {
      this.deleteDialogVisible.set(true);
    }
  }

  protected closeStopDialog(visible: boolean): void {
    this.stopDialogVisible.set(visible);
    if (!visible && !this.stopping()) {
      this.pendingWallet.set(null);
    }
  }

  protected closeDeleteDialog(visible: boolean): void {
    this.deleteDialogVisible.set(visible);
    if (!visible && !this.deleting()) {
      this.pendingWallet.set(null);
    }
  }

  protected confirmStopWallet(): void {
    const wallet = this.pendingWallet();
    if (!wallet || this.stopping()) {
      return;
    }

    this.stopping.set(true);
    this.api
      .stopUserWallet(wallet.userId, { description: 'Stopped by admin from wallet dashboard' })
      .pipe(finalize(() => this.stopping.set(false)))
      .subscribe((transactions) => {
        this.toast.success('Wallet stopped', `${transactions.length} balance${transactions.length === 1 ? '' : 's'} frozen`);
        this.stopDialogVisible.set(false);
        this.pendingWallet.set(null);
        this.loadOverview();
      });
  }

  protected confirmDeleteTransactions(): void {
    const wallet = this.pendingWallet();
    if (!wallet || this.deleting()) {
      return;
    }

    this.deleting.set(true);
    this.api
      .deleteUserWalletTransactions(wallet.userId)
      .pipe(finalize(() => this.deleting.set(false)))
      .subscribe((deletedCount) => {
        this.toast.success('Wallet transactions deleted', `${deletedCount} transaction${deletedCount === 1 ? '' : 's'} removed`);
        this.deleteDialogVisible.set(false);
        this.pendingWallet.set(null);
        this.loadOverview();
      });
  }

  protected exportRows(fileName: string, rows: AdminWalletUserDto[] | AdminWalletTransactionDto[]): void {
    exportCsv(fileName, rows as unknown as ExportRow[]);
  }

  protected exportRowsJson(fileName: string, rows: AdminWalletUserDto[] | AdminWalletTransactionDto[]): void {
    exportJson(fileName, rows as unknown as ExportRow[]);
  }

  private cartesianOptions(): ChartOptions {
    return {
      responsive: true,
      maintainAspectRatio: false,
      plugins: {
        legend: {
          labels: { color: '#64748b', boxWidth: 10, boxHeight: 10, font: { size: 11 } }
        }
      },
      scales: {
        x: {
          grid: { display: false },
          ticks: { color: '#64748b', font: { size: 10 } }
        },
        y: {
          grid: { color: '#e2e8f0' },
          ticks: { color: '#94a3b8', font: { size: 10 } }
        }
      }
    };
  }
}

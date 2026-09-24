import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize, forkJoin, Observable } from 'rxjs';
import { ButtonModule } from 'primeng/button';
import { ChartModule } from 'primeng/chart';
import type { ChartData, ChartOptions } from 'chart.js';

import { AdminDataTable } from '../../../../shared/components/admin-data-table/admin-data-table';
import { AdminFormDialog } from '../../../../shared/components/admin-form-dialog/admin-form-dialog';
import { AdminFormField, AdminTableColumn } from '../../../../shared/models/admin-table.model';
import { AppToastService } from '../../../../core/services/app-toast.service';
import { PermissionService } from '../../../../core/permissions/permission.service';
import { Permissions } from '../../../../core/permissions/permission.catalog';
import { exportCsv, exportJson, ExportRow } from '../../../../shared/utils/export.utils';
import { WalletApiService } from '../../api/wallet-api.service';
import { ChargeWalletCommand, WalletBalanceDto, WalletFundsCommand, WalletTransactionDto } from '../../models/wallet.models';

type WalletAction = 'deposit' | 'withdraw';

interface WalletActionConfig {
  readonly title: string;
  readonly submitLabel: string;
  readonly successTitle: string;
  readonly defaultDescription: string;
}

interface PrimeChartConfig {
  readonly data: ChartData;
  readonly options: ChartOptions;
}

interface CurrencyAmount {
  readonly currencyCode: string;
  readonly amount: number;
}

@Component({
  selector: 'app-wallet-home',
  imports: [
    DecimalPipe,
    ReactiveFormsModule,
    ButtonModule,
    ChartModule,
    AdminDataTable,
    AdminFormDialog
  ],
  template: `
    <section class="surface-card dashboard-section mb-3">
      <div class="flex flex-col gap-3 lg:flex-row lg:items-start lg:justify-between">
        <div>
          <div class="text-[11px] font-semibold uppercase tracking-wide text-slate-500">Wallet summary</div>
          <h2 class="m-0 mt-1 text-sm font-semibold text-slate-950">Balances and actions</h2>
          <p class="m-0 mt-1 text-[12px] text-slate-500">
            Deposit, withdraw, or freeze wallet funds in SHAMCASH and USDT.
          </p>
        </div>

        @if (canCreate() || canUpdate()) {
          <div class="flex flex-wrap gap-2">
            @if (canCreate()) {
              <p-button
                label="Deposit"
                icon="pi pi-plus"
                size="small"
                (onClick)="openActionForm('deposit')"
              />
            }
            @if (canUpdate()) {
              <p-button
                label="Withdraw"
                icon="pi pi-arrow-up-right"
                size="small"
                severity="secondary"
                [outlined]="true"
                (onClick)="openActionForm('withdraw')"
              />
              <p-button
                label="Freeze all"
                icon="pi pi-lock"
                size="small"
                severity="warn"
                [outlined]="true"
                [disabled]="!canFreezeAll() || freezingAll()"
                [loading]="freezingAll()"
                (onClick)="freezeAll()"
              />
              @if (canUnfreezeAll()) {
                <p-button
                  label="Unfreeze all"
                  icon="pi pi-lock-open"
                  size="small"
                  severity="success"
                  [outlined]="true"
                  [disabled]="unfreezingAll()"
                  [loading]="unfreezingAll()"
                  (onClick)="unfreezeAll()"
                />
              }
            }
          </div>
        }
      </div>

      <div class="mt-2 grid gap-2 md:grid-cols-3">
        @for (item of balances(); track item.currencyCode) {
          <article class="rounded-xl border border-slate-200 bg-slate-50 px-3 py-2">
            <div class="text-[11px] text-slate-500">{{ item.currencyCode }}</div>
            <div class="mt-1 text-lg font-semibold text-slate-900">{{ item.balance | number: '1.2-2' }}</div>
          </article>
        } @empty {
          <article class="rounded-xl border border-slate-200 bg-slate-50 px-3 py-2 text-[12px] text-slate-500">
            No wallet balances yet.
          </article>
        }
      </div>
    </section>

    <section class="mb-3 grid gap-3 xl:grid-cols-[minmax(0,1fr)_minmax(0,0.75fr)]">
      <article class="surface-card dashboard-section">
        <div class="mb-2 flex items-center justify-between">
          <div>
            <div class="text-[11px] font-semibold uppercase tracking-wide text-slate-500">Wallet charts</div>
            <div class="mt-1 text-sm font-semibold text-slate-950">Balance by currency</div>
          </div>
          <span class="rounded-full bg-slate-100 px-2 py-1 text-[10px] font-semibold text-slate-600">
            {{ balances().length }} currencies
          </span>
        </div>
        <p-chart
          type="bar"
          styleClass="dashboard-prime-chart block"
          height="220px"
          [data]="balanceChart().data"
          [options]="balanceChart().options"
        />
      </article>

      <article class="surface-card dashboard-section">
        <div class="mb-2">
          <div class="text-[11px] font-semibold uppercase tracking-wide text-slate-500">Transaction mix</div>
          <div class="mt-1 text-sm font-semibold text-slate-950">Ledger activity</div>
        </div>
        <p-chart
          type="doughnut"
          styleClass="dashboard-prime-chart block"
          height="220px"
          [data]="transactionMixChart().data"
          [options]="transactionMixChart().options"
        />
      </article>

      <article class="surface-card dashboard-section xl:col-span-2">
        <div class="mb-2">
          <div class="text-[11px] font-semibold uppercase tracking-wide text-slate-500">Recent movement</div>
          <div class="mt-1 text-sm font-semibold text-slate-950">Signed wallet transactions</div>
        </div>
        <p-chart
          type="line"
          styleClass="dashboard-prime-chart block"
          height="220px"
          [data]="activityChart().data"
          [options]="activityChart().options"
        />
      </article>
    </section>

    <app-admin-data-table
      title="Wallet transactions"
      subtitle="Track deposits, withdrawals, frozen funds, and wallet history for the current user."
      [columns]="columns"
      [value]="transactions()"
      [loading]="loading()"
      [lazy]="false"
      [rows]="25"
      [totalRecords]="transactions().length"
      [globalFilterFields]="globalFilterFields"
      [showCreate]="canCreate()"
      [showExport]="canRead()"
      createLabel="Deposit"
      searchPlaceholder="Search transactions"
      emptyTitle="No wallet transactions"
      emptyMessage="Deposit into your wallet to create the first transaction."
      (refresh)="loadData()"
      (create)="openActionForm('deposit')"
      (exportCsv)="exportRows('wallet-transactions', $event)"
      (exportJson)="exportRowsJson('wallet-transactions', $event)"
    />

    <app-admin-form-dialog
      [visible]="actionVisible()"
      [title]="actionTitle()"
      [form]="actionForm"
      [fields]="actionFields"
      [submitLabel]="actionSubmitLabel()"
      [loading]="savingAction()"
      (visibleChange)="closeActionForm($event)"
      (submit)="submitAction()"
    />
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class WalletHome {
  private readonly api = inject(WalletApiService);
  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly toast = inject(AppToastService);
  private readonly permissionService = inject(PermissionService);

  protected readonly loading = signal(false);
  protected readonly savingAction = signal(false);
  protected readonly freezingAll = signal(false);
  protected readonly unfreezingAll = signal(false);
  protected readonly actionVisible = signal(false);
  protected readonly activeAction = signal<WalletAction>('deposit');
  protected readonly balances = signal<WalletBalanceDto[]>([]);
  protected readonly transactions = signal<WalletTransactionDto[]>([]);

  protected readonly canRead = computed(() =>
    this.permissionService.can({ any: [Permissions.wallet.read] })
  );
  protected readonly canCreate = computed(() =>
    this.permissionService.can({ any: [Permissions.wallet.create] })
  );
  protected readonly canUpdate = computed(() =>
    this.permissionService.can({ any: [Permissions.wallet.update] })
  );

  protected readonly globalFilterFields = ['currencyCode', 'transactionType', 'description', 'referenceId'];
  protected readonly columns: AdminTableColumn[] = [
    { field: 'id', header: 'ID', sortable: true },
    { field: 'currencyCode', header: 'Currency', sortable: true, filter: true },
    { field: 'amount', header: 'Amount', sortable: true, cellType: 'currency', currencyCode: 'USD' },
    { field: 'transactionType', header: 'Type', sortable: true, filter: true },
    { field: 'description', header: 'Description', filter: true },
    { field: 'referenceId', header: 'Reference', filter: true },
    { field: 'createdAt', header: 'Created', sortable: true, cellType: 'date' }
  ];

  protected readonly actionFields: AdminFormField[] = [
    {
      key: 'currencyCode',
      label: 'Currency',
      type: 'select',
      required: true,
      options: [
        { label: 'SHAMCASH', value: 'SHAMCASH' },
        { label: 'USDT', value: 'USDT' }
      ]
    },
    { key: 'amount', label: 'Amount', type: 'number', required: true },
    { key: 'description', label: 'Description', type: 'textarea' }
  ];

  protected readonly actionForm = this.formBuilder.group({
    currencyCode: ['SHAMCASH', Validators.required],
    amount: [0, [Validators.required, Validators.min(0.01)]],
    description: ['']
  });

  protected readonly actionConfig = computed<WalletActionConfig>(() => {
    switch (this.activeAction()) {
      case 'withdraw':
        return {
          title: 'Withdraw wallet funds',
          submitLabel: 'Withdraw',
          successTitle: 'Wallet withdrawal created',
          defaultDescription: 'Wallet withdrawal'
        };
      default:
        return {
          title: 'Deposit wallet funds',
          submitLabel: 'Deposit',
          successTitle: 'Wallet deposit created',
          defaultDescription: 'Wallet deposit'
        };
    }
  });

  protected readonly actionTitle = computed(() => this.actionConfig().title);
  protected readonly actionSubmitLabel = computed(() => this.actionConfig().submitLabel);
  protected readonly positiveBalances = computed(() =>
    this.balances().filter((item) => item.balance > 0)
  );
  protected readonly canFreezeAll = computed(() => this.positiveBalances().length > 0);
  protected readonly frozenBalances = computed<readonly CurrencyAmount[]>(() => {
    const frozen = new Map<string, number>();

    for (const transaction of this.transactions()) {
      if (transaction.transactionType === 'Freeze') {
        frozen.set(
          transaction.currencyCode,
          (frozen.get(transaction.currencyCode) ?? 0) + Math.abs(transaction.amount)
        );
      } else if (transaction.transactionType === 'Unfreeze') {
        frozen.set(
          transaction.currencyCode,
          (frozen.get(transaction.currencyCode) ?? 0) - Math.abs(transaction.amount)
        );
      }
    }

    return Array.from(frozen.entries())
      .map(([currencyCode, amount]) => ({ currencyCode, amount }))
      .filter((item) => item.amount > 0);
  });
  protected readonly canUnfreezeAll = computed(() => this.frozenBalances().length > 0);

  protected readonly balanceChart = computed<PrimeChartConfig>(() => ({
    data: {
      labels: this.balances().map((item) => item.currencyCode),
      datasets: [
        {
          label: 'Balance',
          data: this.balances().map((item) => item.balance),
          backgroundColor: ['#14b8a6', '#0f172a', '#6366f1', '#f59e0b'],
          borderRadius: 8,
          borderSkipped: false
        }
      ]
    },
    options: {
      responsive: true,
      maintainAspectRatio: false,
      plugins: {
        legend: { display: false },
        tooltip: {
          callbacks: {
            label: (context) => `${context.dataset.label}: ${context.formattedValue}`
          }
        }
      },
      scales: {
        x: {
          grid: { display: false },
          ticks: { color: '#64748b', font: { size: 10 } }
        },
        y: {
          beginAtZero: true,
          grid: { color: '#e2e8f0' },
          ticks: { color: '#94a3b8', font: { size: 10 } }
        }
      }
    }
  }));

  protected readonly transactionMixChart = computed<PrimeChartConfig>(() => {
    const buckets = new Map<string, number>();

    for (const transaction of this.transactions()) {
      buckets.set(
        transaction.transactionType,
        (buckets.get(transaction.transactionType) ?? 0) + Math.abs(transaction.amount)
      );
    }

    const labels = Array.from(buckets.keys());
    const values = Array.from(buckets.values());

    return {
      data: {
        labels: labels.length ? labels : ['No activity'],
        datasets: [
          {
            data: values.length ? values : [1],
            backgroundColor: ['#14b8a6', '#ef4444', '#f59e0b', '#6366f1', '#94a3b8'],
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
    };
  });

  protected readonly activityChart = computed<PrimeChartConfig>(() => {
    const items = [...this.transactions()]
      .sort((left, right) => new Date(left.createdAt).getTime() - new Date(right.createdAt).getTime())
      .slice(-12);

    return {
      data: {
        labels: items.map((item) => this.formatChartDate(item.createdAt)),
        datasets: [
          {
            label: 'Amount',
            data: items.map((item) => item.amount),
            borderColor: '#14b8a6',
            backgroundColor: 'rgba(20, 184, 166, 0.14)',
            pointBackgroundColor: '#14b8a6',
            pointBorderColor: '#ffffff',
            pointRadius: 3,
            pointHoverRadius: 6,
            tension: 0.35,
            fill: true
          }
        ]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: { display: false },
          tooltip: {
            callbacks: {
              label: (context) => `${context.formattedValue}`
            }
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
      }
    };
  });

  constructor() {
    this.loadData();
  }

  protected loadData(): void {
    this.loading.set(true);
    this.api
      .getBalance()
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe((items) => this.balances.set(items));

    this.api.getTransactions().subscribe((items) => this.transactions.set(items));
  }

  protected openActionForm(action: WalletAction): void {
    this.activeAction.set(action);
    this.actionForm.reset({
      currencyCode: 'SHAMCASH',
      amount: 0,
      description: this.actionConfig().defaultDescription
    });
    this.actionVisible.set(true);
  }

  protected closeActionForm(visible: boolean): void {
    this.actionVisible.set(visible);
  }

  protected submitAction(): void {
    if (this.actionForm.invalid || this.savingAction()) {
      return;
    }

    const value = this.actionForm.getRawValue();
    const payload: WalletFundsCommand = {
      currencyCode: value.currencyCode.trim().toUpperCase(),
      amount: value.amount,
      description: value.description || null
    };

    this.savingAction.set(true);
    this.submitWalletAction(payload)
      .pipe(finalize(() => this.savingAction.set(false)))
      .subscribe(() => {
        this.toast.success(this.actionConfig().successTitle, `${payload.amount} ${payload.currencyCode}`);
        this.actionVisible.set(false);
        this.loadData();
      });
  }

  protected freezeAll(): void {
    if (this.freezingAll()) {
      return;
    }

    const balances = this.positiveBalances();

    if (!balances.length) {
      this.toast.warn('Nothing to freeze', 'There are no positive wallet balances.');
      return;
    }

    const requests = balances.map((item) =>
      this.api.freeze({
        currencyCode: item.currencyCode,
        amount: item.balance,
        description: 'Frozen full wallet balance'
      })
    );

    this.freezingAll.set(true);
    forkJoin(requests)
      .pipe(finalize(() => this.freezingAll.set(false)))
      .subscribe(() => {
        const totalCurrencies = balances.length;
        this.toast.success('Wallet funds frozen', `${totalCurrencies} balance${totalCurrencies === 1 ? '' : 's'} frozen`);
        this.loadData();
      });
  }

  protected unfreezeAll(): void {
    if (this.unfreezingAll()) {
      return;
    }

    const frozenBalances = this.frozenBalances();

    if (!frozenBalances.length) {
      this.toast.warn('Nothing to unfreeze', 'There are no frozen wallet funds.');
      return;
    }

    const requests = frozenBalances.map((item) =>
      this.api.unfreeze({
        currencyCode: item.currencyCode,
        amount: item.amount,
        description: 'Unfrozen full wallet balance'
      })
    );

    this.unfreezingAll.set(true);
    forkJoin(requests)
      .pipe(finalize(() => this.unfreezingAll.set(false)))
      .subscribe(() => {
        const totalCurrencies = frozenBalances.length;
        this.toast.success('Wallet funds unfrozen', `${totalCurrencies} balance${totalCurrencies === 1 ? '' : 's'} restored`);
        this.loadData();
      });
  }

  protected exportRows(fileName: string, rows: WalletTransactionDto[]): void {
    exportCsv(fileName, rows as unknown as ExportRow[]);
  }

  protected exportRowsJson(fileName: string, rows: WalletTransactionDto[]): void {
    exportJson(fileName, rows as unknown as ExportRow[]);
  }

  private submitWalletAction(payload: WalletFundsCommand): Observable<WalletTransactionDto> {
    switch (this.activeAction()) {
      case 'withdraw':
        return this.api.withdraw(payload);
      default:
        return this.api.charge(payload as ChargeWalletCommand);
    }
  }

  private formatChartDate(value: string): string {
    return new Date(value).toLocaleDateString(undefined, { month: 'short', day: 'numeric' });
  }
}

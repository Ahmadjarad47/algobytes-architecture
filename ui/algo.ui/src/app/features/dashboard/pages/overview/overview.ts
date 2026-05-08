import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { ChartModule } from 'primeng/chart';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import type { ChartData, ChartOptions } from 'chart.js';

import { AccessPolicyAdminDto } from '../../../access-policies/models/access-policies.models';
import { ErrorLogDto } from '../../../error-logs/models/error-logs.models';
import { ApplicationLogDto } from '../../../logs/models/logs.models';
import { DashboardApiService } from '../../api/dashboard-api.service';
import { UserDashboardStats } from '../../models/dashboard.models';

interface DashboardMetric {
  readonly label: string;
  readonly value: string;
  readonly helper: string;
  readonly trend: string;
  readonly icon: string;
  readonly tone: 'primary' | 'success' | 'warn' | 'danger';
  readonly sparkline: readonly number[];
}

interface ChartSegment {
  readonly label: string;
  readonly value: number;
  readonly ratio: number;
  readonly className: string;
}

interface TimelineBucket {
  readonly label: string;
  readonly logs: number;
  readonly errors: number;
  readonly logRatio: number;
  readonly errorRatio: number;
}

interface PrimeChartConfig {
  readonly data: ChartData;
  readonly options: ChartOptions;
}

@Component({
  selector: 'app-overview',
  imports: [CommonModule, RouterLink, CardModule, ChartModule, TagModule, TableModule, ButtonModule, DatePipe],
  template: `
    <div class="dashboard-grid">
      <section class="dashboard-grid lg:grid-cols-[minmax(0,1fr)_320px]">
        <div class="surface-card dashboard-section">
          <div class="flex flex-col gap-3 xl:flex-row xl:items-start xl:justify-between">
            <div class="min-w-0">
              <div class="eyebrow">Operations summary</div>
              <div class="mt-1 flex flex-wrap items-center gap-2">
                <h2 class="m-0 text-[18px] font-semibold leading-tight text-slate-950">
                  Control plane snapshot
                </h2>
                <p-tag value="Live data" severity="success" />
              </div>
              <p class="m-0 mt-1 max-w-2xl text-[12px] text-slate-500">
                Identity, access, operational events, and incident signals pulled into one compact
                workspace view.
              </p>
            </div>

            <div class="grid min-w-[280px] gap-2 sm:grid-cols-3">
              <div class="rounded-xl border border-slate-200 bg-slate-50 px-3 py-2">
                <div class="text-[10px] font-semibold uppercase tracking-wide text-slate-500">
                  API health
                </div>
                <div class="mt-1 text-sm font-semibold text-slate-950">{{ apiHealthLabel() }}</div>
                <div class="mt-0.5 text-[11px] text-slate-500">{{ healthHint() }}</div>
              </div>
              <div class="rounded-xl border border-slate-200 bg-slate-50 px-3 py-2">
                <div class="text-[10px] font-semibold uppercase tracking-wide text-slate-500">
                  Error volume
                </div>
                <div class="mt-1 text-sm font-semibold text-slate-950">{{ recentErrors().length }}</div>
                <div class="mt-0.5 text-[11px] text-slate-500">Latest incidents in queue</div>
              </div>
              <div class="rounded-xl border border-slate-200 bg-slate-50 px-3 py-2">
                <div class="text-[10px] font-semibold uppercase tracking-wide text-slate-500">
                  Policy coverage
                </div>
                <div class="mt-1 text-sm font-semibold text-slate-950">{{ enabledPolicyCount() }}</div>
                <div class="mt-0.5 text-[11px] text-slate-500">Enabled access rules</div>
              </div>
            </div>
          </div>

          <div class="mt-4 flex flex-col gap-3 rounded-xl border border-slate-200 bg-slate-50/70 p-3 xl:flex-row xl:items-end xl:justify-between">
            <div class="min-w-0">
              <div class="text-[10px] font-semibold uppercase tracking-wide text-slate-500">
                Date filtration
              </div>
              <div class="mt-1 text-sm font-semibold text-slate-950">{{ filterSummary() }}</div>
            </div>

            <div class="grid min-w-0 flex-1 gap-2 sm:grid-cols-2 xl:max-w-[460px]">
              <label class="dashboard-filter-field">
                <span>From</span>
                <input
                  type="date"
                  [value]="fromDate()"
                  (change)="setFromDate($any($event.target).value)"
                />
              </label>
              <label class="dashboard-filter-field">
                <span>To</span>
                <input
                  type="date"
                  [value]="toDate()"
                  (change)="setToDate($any($event.target).value)"
                />
              </label>
            </div>

            <div class="flex flex-wrap gap-2">
              <button type="button" class="dashboard-filter-button" (click)="setQuickRange(1)">
                24h
              </button>
              <button type="button" class="dashboard-filter-button" (click)="setQuickRange(7)">
                7d
              </button>
              <button type="button" class="dashboard-filter-button" (click)="setQuickRange(30)">
                30d
              </button>
              <button type="button" class="dashboard-filter-button is-primary" (click)="loadOverview()">
                Refresh
              </button>
              <button type="button" class="dashboard-filter-button" (click)="resetDateFilter()">
                Reset
              </button>
            </div>
          </div>

          <div class="mt-4 grid gap-3 xl:grid-cols-[minmax(260px,0.9fr)_minmax(320px,1.1fr)_minmax(300px,1fr)]">
            <section class="snapshot-panel">
              <div class="flex items-start justify-between gap-3">
                <div>
                  <div class="text-[10px] font-semibold uppercase tracking-wide text-slate-500">
                    Users
                  </div>
                  <div class="mt-1 text-sm font-semibold text-slate-950">Directory state</div>
                </div>
                <span class="rounded-full bg-slate-100 px-2 py-1 text-[10px] font-semibold text-slate-600">
                  {{ stats()?.totalUsers ?? 0 }} total
                </span>
              </div>

              <div class="mt-3 grid items-center gap-3 sm:grid-cols-[160px_minmax(0,1fr)]">
                <p-chart
                  type="doughnut"
                  styleClass="dashboard-prime-chart"
                  height="160px"
                  [data]="userStateChart().data"
                  [options]="userStateChart().options"
                />

                <div class="grid gap-2">
                  @for (segment of userSegments(); track segment.label) {
                    <div class="chart-row">
                      <span class="chart-key" [class]="segment.className"></span>
                      <span class="min-w-0 flex-1 truncate">{{ segment.label }}</span>
                      <strong>{{ segment.value }}</strong>
                    </div>
                  }
                </div>
              </div>
            </section>

            <section class="snapshot-panel">
              <div class="flex items-start justify-between gap-3">
                <div>
                  <div class="text-[10px] font-semibold uppercase tracking-wide text-slate-500">
                    Health
                  </div>
                  <div class="mt-1 text-sm font-semibold text-slate-950">Latency bands</div>
                </div>
                <p-tag [value]="apiHealthLabel()" [severity]="apiHealthSeverity()" />
              </div>

              <div class="mt-3 grid gap-3 sm:grid-cols-[minmax(0,1fr)_118px]">
                <p-chart
                  type="bar"
                  styleClass="dashboard-prime-chart"
                  height="138px"
                  [data]="latencyBandsChart().data"
                  [options]="latencyBandsChart().options"
                />

                <p-chart
                  type="doughnut"
                  styleClass="dashboard-prime-chart"
                  height="136px"
                  [data]="healthGaugeChart().data"
                  [options]="healthGaugeChart().options"
                />
              </div>
            </section>

            <section class="snapshot-panel">
              <div class="flex items-start justify-between gap-3">
                <div>
                  <div class="text-[10px] font-semibold uppercase tracking-wide text-slate-500">
                    Logs
                  </div>
                  <div class="mt-1 text-sm font-semibold text-slate-950">Signal flow</div>
                </div>
                <span class="rounded-full bg-slate-100 px-2 py-1 text-[10px] font-semibold text-slate-600">
                  {{ logs().length }} events
                </span>
              </div>

              <div class="mt-3 grid gap-3">
                <p-chart
                  type="bar"
                  styleClass="dashboard-prime-chart"
                  height="136px"
                  [data]="logLevelChart().data"
                  [options]="logLevelChart().options"
                />

                <div class="grid grid-cols-3 gap-2">
                  <div class="mini-stat">
                    <span>5xx</span>
                    <strong>{{ serverErrorCount() }}</strong>
                  </div>
                  <div class="mini-stat">
                    <span>Incidents</span>
                    <strong>{{ recentErrors().length }}</strong>
                  </div>
                  <div class="mini-stat">
                    <span>Ratio</span>
                    <strong>{{ incidentRatioLabel() }}</strong>
                  </div>
                </div>
              </div>
            </section>
          </div>

          <section class="snapshot-panel mt-3">
            <div class="flex flex-col gap-2 sm:flex-row sm:items-start sm:justify-between">
              <div>
                <div class="text-[10px] font-semibold uppercase tracking-wide text-slate-500">
                  Activity timeline
                </div>
                <div class="mt-1 text-sm font-semibold text-slate-950">Logs and failures over selected range</div>
              </div>
              <span class="rounded-full bg-slate-100 px-2 py-1 text-[10px] font-semibold text-slate-600">
                {{ timelineTotal() }} signals
              </span>
            </div>

            <p-chart
              type="line"
              styleClass="dashboard-prime-chart mt-3 block"
              height="260px"
              [data]="activityTimelineChart().data"
              [options]="activityTimelineChart().options"
            />
          </section>
        </div>

        <section class="surface-card dashboard-section">
          <div class="mb-2 flex items-center justify-between">
            <div>
              <div class="text-[11px] font-semibold uppercase tracking-wide text-slate-500">
                Quick actions
              </div>
              <div class="mt-1 text-sm font-semibold text-slate-950">Admin workflows</div>
            </div>
            <i class="pi pi-bolt text-sm text-slate-400"></i>
          </div>

          <div class="grid gap-2">
            @for (action of quickActions; track action.label) {
              <a
                [routerLink]="action.path"
                class="flex items-center gap-2 rounded-xl border border-slate-200 bg-slate-50 px-3 py-2 text-left transition hover:border-slate-300 hover:bg-white"
              >
                <span
                  class="flex h-8 w-8 items-center justify-center rounded-lg bg-white text-slate-600 shadow-sm"
                >
                  <i [class]="action.icon" class="text-[11px]"></i>
                </span>
                <span class="min-w-0 flex-1">
                  <span class="block text-[12px] font-semibold text-slate-900">{{ action.label }}</span>
                  <span class="block truncate text-[11px] text-slate-500">{{ action.description }}</span>
                </span>
              </a>
            }
          </div>
        </section>
      </section>

      <section class="grid gap-3 xl:grid-cols-5">
        @for (metric of metrics(); track metric.label) {
          <article class="surface-card dashboard-section xl:col-span-1">
            <div class="flex items-start justify-between gap-2">
              <div class="min-w-0">
                <div class="text-[10px] font-semibold uppercase tracking-wide text-slate-500">
                  {{ metric.label }}
                </div>
                <div class="mt-1 text-[20px] font-semibold leading-none text-slate-950">
                  {{ metric.value }}
                </div>
                <div class="mt-1 text-[11px] text-slate-500">{{ metric.helper }}</div>
              </div>
              <span
                class="flex h-8 w-8 items-center justify-center rounded-lg"
                [class]="metricToneClass(metric.tone)"
              >
                <i [class]="metric.icon" class="text-[12px]"></i>
              </span>
            </div>

            <div class="mt-3 flex items-end justify-between gap-3">
              <div class="metric-sparkline">
                @for (bar of metric.sparkline; track $index) {
                  <span [style.height.px]="bar"></span>
                }
              </div>
              <div class="rounded-full bg-slate-100 px-2 py-1 text-[10px] font-semibold text-slate-600">
                {{ metric.trend }}
              </div>
            </div>
          </article>
        }
      </section>

      <section class="dashboard-grid xl:grid-cols-[minmax(0,1.75fr)_minmax(320px,0.95fr)]">
        <div class="dashboard-grid">
          <section class="surface-card dashboard-section">
            <div class="mb-3 flex items-center justify-between">
              <div>
                <div class="text-[11px] font-semibold uppercase tracking-wide text-slate-500">
                  User activity
                </div>
                <h3 class="m-0 mt-1 text-sm font-semibold text-slate-950">Recent onboarding and account changes</h3>
              </div>
              <p-button
                icon="pi pi-users"
                label="Directory"
                size="small"
                severity="secondary"
                [outlined]="true"
                [routerLink]="['/users']"
              />
            </div>

            <div class="grid gap-3 lg:grid-cols-2">
              <div class="rounded-xl border border-slate-200 bg-slate-50/80 p-2.5">
                <div class="mb-2 flex items-center justify-between">
                  <div class="text-[11px] font-semibold text-slate-800">New users</div>
                  <span class="text-[10px] text-slate-500">Latest additions</span>
                </div>
                <div class="space-y-1.5">
                  @for (user of recentUsers(); track user.userId) {
                    <div class="flex items-center gap-2 rounded-lg bg-white px-2.5 py-2 shadow-sm">
                      <div
                        class="flex h-8 w-8 flex-none items-center justify-center rounded-lg bg-slate-100 text-[11px] font-semibold text-slate-600"
                      >
                        {{ user.displayName.slice(0, 1).toUpperCase() }}
                      </div>
                      <div class="min-w-0 flex-1">
                        <div class="truncate text-[12px] font-semibold text-slate-900">{{ user.displayName }}</div>
                        <div class="truncate text-[11px] text-slate-500">{{ user.email }}</div>
                      </div>
                      <div class="text-[10px] text-slate-400">
                        {{ user.occurredAt | date: 'MMM d' }}
                      </div>
                    </div>
                  } @empty {
                    <div class="rounded-lg bg-white px-2.5 py-3 text-[11px] text-slate-500">
                      No recent user activity available.
                    </div>
                  }
                </div>
              </div>

              <div class="rounded-xl border border-slate-200 bg-slate-50/80 p-2.5">
                <div class="mb-2 flex items-center justify-between">
                  <div class="text-[11px] font-semibold text-slate-800">Locked accounts</div>
                  <span class="text-[10px] text-slate-500">Needs review</span>
                </div>
                <div class="space-y-1.5">
                  @for (user of lockedUsers(); track user.userId) {
                    <div class="flex items-center gap-2 rounded-lg bg-white px-2.5 py-2 shadow-sm">
                      <div
                        class="flex h-8 w-8 flex-none items-center justify-center rounded-lg bg-rose-50 text-[11px] font-semibold text-rose-600"
                      >
                        {{ user.displayName.slice(0, 1).toUpperCase() }}
                      </div>
                      <div class="min-w-0 flex-1">
                        <div class="truncate text-[12px] font-semibold text-slate-900">{{ user.displayName }}</div>
                        <div class="truncate text-[11px] text-slate-500">{{ user.email }}</div>
                      </div>
                      <p-tag value="Locked" severity="danger" />
                    </div>
                  } @empty {
                    <div class="rounded-lg bg-white px-2.5 py-3 text-[11px] text-slate-500">
                      No locked accounts in the latest sample.
                    </div>
                  }
                </div>
              </div>
            </div>
          </section>

          <section class="surface-card dashboard-section">
            <div class="mb-3 flex items-center justify-between">
              <div>
                <div class="text-[11px] font-semibold uppercase tracking-wide text-slate-500">
                  Recent logs
                </div>
                <h3 class="m-0 mt-1 text-sm font-semibold text-slate-950">Event stream preview</h3>
              </div>
              <p-button
                icon="pi pi-arrow-right"
                label="Open logs"
                size="small"
                severity="secondary"
                [outlined]="true"
                [routerLink]="['/logs']"
              />
            </div>

            <p-table
              [value]="recentLogs()"
              styleClass="p-datatable-sm"
              tableStyleClass="min-w-full"
              responsiveLayout="scroll"
            >
              <ng-template #header>
                <tr>
                  <th>Timestamp</th>
                  <th>Level</th>
                  <th>Message</th>
                  <th>Route</th>
                  <th>Status</th>
                </tr>
              </ng-template>

              <ng-template #body let-log>
                <tr>
                  <td class="whitespace-nowrap text-[11px] text-slate-500">
                    {{ log.timestamp | date: 'MMM d, HH:mm' }}
                  </td>
                  <td>
                    <p-tag [value]="log.level" [severity]="logLevelSeverity(log.level)" />
                  </td>
                  <td>
                    <div class="max-w-[380px] truncate text-[12px] font-medium text-slate-800">
                      {{ log.message }}
                    </div>
                  </td>
                  <td class="text-[11px] text-slate-500">
                    {{ log.requestMethod || '-' }} {{ log.requestPath || '' }}
                  </td>
                  <td>
                    <span class="text-[11px] font-medium text-slate-700">
                      {{ log.statusCode ?? '-' }}
                    </span>
                  </td>
                </tr>
              </ng-template>

              <ng-template #emptymessage>
                <tr>
                  <td colspan="5" class="px-3 py-6 text-center text-[11px] text-slate-500">
                    No logs returned from the latest request.
                  </td>
                </tr>
              </ng-template>
            </p-table>
          </section>
        </div>

        <div class="dashboard-grid">
          <section class="surface-card dashboard-section">
            <div class="mb-2 flex items-center justify-between">
              <div>
                <div class="text-[11px] font-semibold uppercase tracking-wide text-slate-500">
                  Policy summary
                </div>
                <h3 class="m-0 mt-1 text-sm font-semibold text-slate-950">Security posture</h3>
              </div>
              <i class="pi pi-shield text-sm text-slate-400"></i>
            </div>

            <div class="grid gap-2 sm:grid-cols-2">
              <div class="rounded-xl border border-slate-200 bg-slate-50 px-3 py-2">
                <div class="text-[10px] uppercase tracking-wide text-slate-500">Enabled</div>
                <div class="mt-1 text-lg font-semibold text-slate-950">{{ enabledPolicyCount() }}</div>
              </div>
              <div class="rounded-xl border border-slate-200 bg-slate-50 px-3 py-2">
                <div class="text-[10px] uppercase tracking-wide text-slate-500">Disabled</div>
                <div class="mt-1 text-lg font-semibold text-slate-950">{{ disabledPolicyCount() }}</div>
              </div>
              <div class="rounded-xl border border-slate-200 bg-slate-50 px-3 py-2">
                <div class="text-[10px] uppercase tracking-wide text-slate-500">Conditional</div>
                <div class="mt-1 text-lg font-semibold text-slate-950">{{ conditionalPolicyCount() }}</div>
              </div>
              <div class="rounded-xl border border-slate-200 bg-slate-50 px-3 py-2">
                <div class="text-[10px] uppercase tracking-wide text-slate-500">High priority</div>
                <div class="mt-1 text-lg font-semibold text-slate-950">{{ highPriorityPolicyCount() }}</div>
              </div>
            </div>
          </section>

          <section class="surface-card dashboard-section">
            <div class="mb-3 flex items-center justify-between">
              <div>
                <div class="text-[11px] font-semibold uppercase tracking-wide text-slate-500">
                  Roles distribution
                </div>
                <h3 class="m-0 mt-1 text-sm font-semibold text-slate-950">Assignment mix</h3>
              </div>
              <span class="text-[10px] text-slate-500">{{ totalRoleAssignments() }} assignments</span>
            </div>

            <div class="space-y-2">
              @for (role of roleEntries(); track role.label) {
                <div class="space-y-1">
                  <div class="flex items-center justify-between text-[11px]">
                    <span class="font-medium text-slate-700">{{ role.label }}</span>
                    <span class="text-slate-500">{{ role.value }}</span>
                  </div>
                  <div class="h-2 rounded-full bg-slate-100">
                    <div
                      class="h-2 rounded-full bg-slate-900"
                      [style.width.%]="role.ratio"
                    ></div>
                  </div>
                </div>
              } @empty {
                <div class="text-[11px] text-slate-500">No role distribution data available.</div>
              }
            </div>
          </section>

          <section class="surface-card dashboard-section">
            <div class="mb-3 flex items-center justify-between">
              <div>
                <div class="text-[11px] font-semibold uppercase tracking-wide text-slate-500">
                  System health
                </div>
                <h3 class="m-0 mt-1 text-sm font-semibold text-slate-950">Latency and signal quality</h3>
              </div>
              <p-tag [value]="apiHealthLabel()" [severity]="apiHealthSeverity()" />
            </div>

            <div class="grid gap-2">
              <div class="flex items-center justify-between rounded-xl border border-slate-200 bg-slate-50 px-3 py-2">
                <span class="text-[11px] text-slate-500">Average response</span>
                <span class="text-[12px] font-semibold text-slate-900">{{ averageLatency() }} ms</span>
              </div>
              <div class="flex items-center justify-between rounded-xl border border-slate-200 bg-slate-50 px-3 py-2">
                <span class="text-[11px] text-slate-500">Recent 5xx responses</span>
                <span class="text-[12px] font-semibold text-slate-900">{{ serverErrorCount() }}</span>
              </div>
              <div class="flex items-center justify-between rounded-xl border border-slate-200 bg-slate-50 px-3 py-2">
                <span class="text-[11px] text-slate-500">Incident ratio</span>
                <span class="text-[12px] font-semibold text-slate-900">{{ incidentRatioLabel() }}</span>
              </div>
            </div>
          </section>

          <section class="surface-card dashboard-section">
            <div class="mb-3 flex items-center justify-between">
              <div>
                <div class="text-[11px] font-semibold uppercase tracking-wide text-slate-500">
                  Recent errors
                </div>
                <h3 class="m-0 mt-1 text-sm font-semibold text-slate-950">Latest failures</h3>
              </div>
              <p-button
                icon="pi pi-external-link"
                severity="secondary"
                size="small"
                [outlined]="true"
                [routerLink]="['/error-logs']"
              />
            </div>

            <div class="space-y-1.5">
              @for (error of recentErrors(); track error.id ?? error.traceId ?? $index) {
                <div class="rounded-xl border border-slate-200 bg-slate-50 px-3 py-2">
                  <div class="flex items-start justify-between gap-2">
                    <div class="min-w-0">
                      <div class="truncate text-[12px] font-semibold text-slate-900">{{ error.exceptionType }}</div>
                      <div class="mt-0.5 line-clamp-2 text-[11px] text-slate-500">{{ error.message }}</div>
                    </div>
                    <p-tag
                      [value]="error.statusCode ? '' + error.statusCode : 'ERR'"
                      severity="danger"
                    />
                  </div>
                  <div class="mt-2 flex items-center justify-between text-[10px] text-slate-400">
                    <span>{{ error.method || '-' }} {{ error.path || '-' }}</span>
                    <span>{{ error.createdAt | date: 'MMM d, HH:mm' }}</span>
                  </div>
                </div>
              } @empty {
                <div class="rounded-xl border border-slate-200 bg-slate-50 px-3 py-3 text-[11px] text-slate-500">
                  No recent error logs returned.
                </div>
              }
            </div>
          </section>
        </div>
      </section>
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class Overview {
  private readonly dashboardApi = inject(DashboardApiService);

  protected readonly loading = signal(true);
  protected readonly fromDate = signal(this.toDateInputValue(this.addDays(new Date(), -7)));
  protected readonly toDate = signal(this.toDateInputValue(new Date()));
  protected readonly stats = signal<UserDashboardStats | null>(null);
  protected readonly logs = signal<ApplicationLogDto[]>([]);
  protected readonly errorLogs = signal<ErrorLogDto[]>([]);
  protected readonly accessPolicies = signal<AccessPolicyAdminDto[]>([]);

  protected readonly quickActions = [
    { label: 'Create user', description: 'Open the directory and add a new account.', path: '/users', icon: 'pi pi-user-plus' },
    { label: 'Review roles', description: 'Audit role assignment and privileges.', path: '/roles', icon: 'pi pi-id-card' },
    { label: 'Inspect policies', description: 'Validate access rules and conditions.', path: '/access-policies', icon: 'pi pi-shield' },
    { label: 'Open error queue', description: 'Investigate the latest application failures.', path: '/error-logs', icon: 'pi pi-exclamation-triangle' }
  ];

  protected readonly metrics = computed<readonly DashboardMetric[]>(() => {
    const stats = this.stats();

    if (!stats) {
      return [];
    }

    const activeRate = stats.totalUsers ? Math.round((stats.activeUsers / stats.totalUsers) * 100) : 0;
    const confirmedRate = stats.totalUsers
      ? Math.round((stats.emailConfirmedUsers / stats.totalUsers) * 100)
      : 0;

    return [
      {
        label: 'Directory coverage',
        value: `${stats.totalUsers}`,
        helper: `${stats.inactiveUsers} inactive accounts`,
        trend: `+${stats.newUsersThisWeek} this week`,
        icon: 'pi pi-users',
        tone: 'primary',
        sparkline: [10, 13, 12, 16, 18, 20, 24]
      },
      {
        label: 'Active footprint',
        value: `${activeRate}%`,
        helper: `${stats.activeUsers} currently active`,
        trend: `${stats.newUsersToday} new today`,
        icon: 'pi pi-bolt',
        tone: 'success',
        sparkline: [8, 10, 11, 14, 15, 16, 18]
      },
      {
        label: 'Locked accounts',
        value: `${stats.lockedUsers}`,
        helper: `${stats.recentlyLockedUsers.length} surfaced recently`,
        trend: stats.lockedUsers > 0 ? 'Needs review' : 'Stable',
        icon: 'pi pi-lock',
        tone: 'danger',
        sparkline: [6, 8, 7, 9, 10, 8, 7]
      },
      {
        label: 'Verified email',
        value: `${confirmedRate}%`,
        helper: `${stats.emailConfirmedUsers} confirmed`,
        trend: `${stats.emailNotConfirmedUsers} pending`,
        icon: 'pi pi-envelope',
        tone: 'warn',
        sparkline: [9, 11, 12, 13, 15, 17, 19]
      },
      {
        label: 'Monthly growth',
        value: `${stats.newUsersThisMonth}`,
        helper: 'New users this month',
        trend: `${stats.newUsersThisWeek} added this week`,
        icon: 'pi pi-chart-line',
        tone: 'primary',
        sparkline: [7, 9, 10, 12, 14, 17, 21]
      }
    ];
  });

  protected readonly roleEntries = computed(() => {
    const entries = Object.entries(this.stats()?.usersByRole ?? {});
    const total = entries.reduce((sum, [, value]) => sum + value, 0);

    return entries
      .map(([label, value]) => ({
        label,
        value,
        ratio: total ? Math.max(8, Math.round((value / total) * 100)) : 0
      }))
      .sort((left, right) => right.value - left.value);
  });

  protected readonly recentUsers = computed(() => this.stats()?.recentUsers ?? []);
  protected readonly lockedUsers = computed(() => this.stats()?.recentlyLockedUsers ?? []);
  protected readonly recentLogs = computed(() => this.logs().slice(0, 6));
  protected readonly recentErrors = computed(() => this.errorLogs().slice(0, 5));

  protected readonly userSegments = computed<readonly ChartSegment[]>(() => {
    const stats = this.stats();

    if (!stats?.totalUsers) {
      return [
        { label: 'Active', value: 0, ratio: 0, className: 'is-success' },
        { label: 'Inactive', value: 0, ratio: 0, className: 'is-muted' },
        { label: 'Locked', value: 0, ratio: 0, className: 'is-danger' }
      ];
    }

    return [
      {
        label: 'Active',
        value: stats.activeUsers,
        ratio: Math.round((stats.activeUsers / stats.totalUsers) * 100),
        className: 'is-success'
      },
      {
        label: 'Inactive',
        value: stats.inactiveUsers,
        ratio: Math.round((stats.inactiveUsers / stats.totalUsers) * 100),
        className: 'is-muted'
      },
      {
        label: 'Locked',
        value: stats.lockedUsers,
        ratio: Math.round((stats.lockedUsers / stats.totalUsers) * 100),
        className: 'is-danger'
      }
    ];
  });

  protected readonly userHealthScore = computed(() => {
    const stats = this.stats();

    if (!stats?.totalUsers) {
      return 0;
    }

    const confirmedRatio = stats.emailConfirmedUsers / stats.totalUsers;
    const activeRatio = stats.activeUsers / stats.totalUsers;
    const lockedPenalty = stats.lockedUsers / stats.totalUsers;

    return Math.max(0, Math.round(((confirmedRatio * 0.55 + activeRatio * 0.45) - lockedPenalty) * 100));
  });

  protected readonly userStateChart = computed<PrimeChartConfig>(() => ({
    data: {
      labels: this.userSegments().map((segment) => segment.label),
      datasets: [
        {
          data: this.userSegments().map((segment) => segment.value),
          backgroundColor: ['#10b981', '#94a3b8', '#ef4444'],
          borderColor: '#ffffff',
          borderWidth: 3,
          hoverBackgroundColor: ['#059669', '#64748b', '#dc2626'],
          hoverOffset: 8
        }
      ]
    },
    options: {
      responsive: true,
      maintainAspectRatio: false,
      cutout: '68%',
      plugins: {
        legend: { display: false },
        tooltip: {
          callbacks: {
            label: (context) => `${context.label}: ${context.formattedValue} users`
          }
        }
      }
    }
  }));

  protected readonly latencyBandsChart = computed<PrimeChartConfig>(() => {
    const fast = this.logs().filter((log) => (log.elapsedMilliseconds ?? 0) > 0 && (log.elapsedMilliseconds ?? 0) < 250).length;
    const watch = this.logs().filter((log) => (log.elapsedMilliseconds ?? 0) >= 250 && (log.elapsedMilliseconds ?? 0) < 700).length;
    const slow = this.logs().filter((log) => (log.elapsedMilliseconds ?? 0) >= 700).length;

    return {
      data: {
        labels: ['<250', '250-699', '700+'],
        datasets: [
          {
            label: 'Requests',
            data: [fast, watch, slow],
            backgroundColor: ['#10b981', '#f59e0b', '#ef4444'],
            borderRadius: 6,
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
              label: (context) => `${context.formattedValue} requests`
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
            ticks: { color: '#94a3b8', precision: 0, font: { size: 10 } }
          }
        }
      }
    };
  });

  protected readonly healthGaugeChart = computed<PrimeChartConfig>(() => {
    const value = Math.min(100, Math.round((this.averageLatency() / 1000) * 100));
    const color = this.apiHealthSeverity() === 'danger'
      ? '#ef4444'
      : this.apiHealthSeverity() === 'warn'
        ? '#f59e0b'
        : '#10b981';

    return {
      data: {
        labels: ['Used', 'Remaining'],
        datasets: [
          {
            data: [value, Math.max(0, 100 - value)],
            backgroundColor: [color, '#e2e8f0'],
            borderColor: '#ffffff',
            borderWidth: 2,
            hoverOffset: 3
          }
        ]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        circumference: 180,
        rotation: 270,
        cutout: '72%',
        plugins: {
          legend: { display: false },
          tooltip: {
            callbacks: {
              label: (context) =>
                context.dataIndex === 0 ? `${this.averageLatency()} ms average` : 'Capacity headroom'
            }
          }
        }
      }
    };
  });

  protected readonly logLevelChart = computed<PrimeChartConfig>(() => {
    const buckets = new Map<string, { value: number; className: string }>([
      ['Info', { value: 0, className: 'is-info' }],
      ['Warn', { value: 0, className: 'is-warn' }],
      ['Error', { value: 0, className: 'is-danger' }],
      ['Other', { value: 0, className: 'is-muted' }]
    ]);

    for (const log of this.logs()) {
      const level = log.level.toLowerCase();

      if (level === 'information' || level === 'info') {
        buckets.get('Info')!.value += 1;
      } else if (level === 'warning' || level === 'warn') {
        buckets.get('Warn')!.value += 1;
      } else if (level === 'error' || level === 'fatal') {
        buckets.get('Error')!.value += 1;
      } else {
        buckets.get('Other')!.value += 1;
      }
    }

    const labels = Array.from(buckets.keys());
    const values = Array.from(buckets.values(), (bucket) => bucket.value);

    return {
      data: {
        labels,
        datasets: [
          {
            label: 'Events',
            data: values,
            backgroundColor: ['#3b82f6', '#f59e0b', '#ef4444', '#94a3b8'],
            borderRadius: 6,
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
              label: (context) => `${context.formattedValue} events`
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
            grid: { display: false },
            ticks: { display: false, precision: 0 }
          }
        }
      }
    };
  });

  protected readonly timelineBuckets = computed<readonly TimelineBucket[]>(() => {
    const from = this.fromFilterDate();
    const to = this.toFilterDate();
    const bucketCount = 7;
    const span = Math.max(1, to.getTime() - from.getTime());
    const bucketSize = span / bucketCount;
    const buckets = Array.from({ length: bucketCount }, (_, index) => {
      const start = new Date(from.getTime() + bucketSize * index);

      return {
        label: this.formatTimelineLabel(start, span),
        logs: 0,
        errors: 0
      };
    });

    for (const log of this.logs()) {
      const index = this.timelineIndex(new Date(log.timestamp), from, bucketSize, bucketCount);
      buckets[index].logs += 1;
    }

    for (const error of this.errorLogs()) {
      const index = this.timelineIndex(new Date(error.timestamp || error.createdAt), from, bucketSize, bucketCount);
      buckets[index].errors += 1;
    }

    const max = Math.max(1, ...buckets.flatMap((bucket) => [bucket.logs, bucket.errors]));

    return buckets.map((bucket) => ({
      ...bucket,
      logRatio: bucket.logs ? Math.max(12, Math.round((bucket.logs / max) * 100)) : 0,
      errorRatio: bucket.errors ? Math.max(12, Math.round((bucket.errors / max) * 100)) : 0
    }));
  });

  protected readonly timelineTotal = computed(() =>
    this.timelineBuckets().reduce((sum, bucket) => sum + bucket.logs + bucket.errors, 0)
  );

  protected readonly activityTimelineChart = computed<PrimeChartConfig>(() => {
    const buckets = this.timelineBuckets();

    return {
      data: {
        labels: buckets.map((bucket) => bucket.label),
        datasets: [
          {
            label: 'Logs',
            data: buckets.map((bucket) => bucket.logs),
            borderColor: '#3b82f6',
            backgroundColor: 'rgba(59, 130, 246, 0.16)',
            pointBackgroundColor: '#3b82f6',
            pointBorderColor: '#ffffff',
            pointRadius: 3,
            pointHoverRadius: 6,
            tension: 0.35,
            fill: true
          },
          {
            label: 'Errors',
            data: buckets.map((bucket) => bucket.errors),
            borderColor: '#ef4444',
            backgroundColor: 'rgba(239, 68, 68, 0.12)',
            pointBackgroundColor: '#ef4444',
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
        interaction: {
          mode: 'index',
          intersect: false
        },
        plugins: {
          legend: {
            display: true,
            position: 'top',
            align: 'end',
            labels: {
              color: '#64748b',
              boxWidth: 10,
              boxHeight: 10,
              usePointStyle: true,
              font: { size: 11 }
            }
          },
          tooltip: {
            mode: 'index',
            intersect: false,
            callbacks: {
              label: (context) => `${context.dataset.label}: ${context.formattedValue} signals`
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
            ticks: { color: '#94a3b8', precision: 0, font: { size: 10 } }
          }
        }
      }
    };
  });

  protected readonly filterSummary = computed(() => {
    const from = this.fromDate();
    const to = this.toDate();

    if (!from && !to) {
      return 'All recent operations';
    }

    if (from && to) {
      return `${from} to ${to}`;
    }

    return from ? `Since ${from}` : `Until ${to}`;
  });

  protected readonly enabledPolicyCount = computed(
    () => this.accessPolicies().filter((policy) => policy.isEnabled).length
  );

  protected readonly disabledPolicyCount = computed(
    () => this.accessPolicies().filter((policy) => !policy.isEnabled).length
  );

  protected readonly conditionalPolicyCount = computed(
    () => this.accessPolicies().filter((policy) => !!policy.conditionJson).length
  );

  protected readonly highPriorityPolicyCount = computed(
    () => this.accessPolicies().filter((policy) => (policy.priority ?? 0) >= 50).length
  );

  protected readonly totalRoleAssignments = computed(() =>
    this.roleEntries().reduce((sum, role) => sum + role.value, 0)
  );

  protected readonly averageLatency = computed(() => {
    const samples = this.logs()
      .map((log) => log.elapsedMilliseconds ?? 0)
      .filter((value) => value > 0);

    if (!samples.length) {
      return 0;
    }

    return Math.round(samples.reduce((sum, value) => sum + value, 0) / samples.length);
  });

  protected readonly serverErrorCount = computed(
    () => this.logs().filter((log) => (log.statusCode ?? 0) >= 500).length
  );

  protected readonly apiHealthSeverity = computed<
    'success' | 'info' | 'warn' | 'danger' | 'secondary' | 'contrast'
  >(() => {
    const errors = this.serverErrorCount();
    const latency = this.averageLatency();

    if (errors >= 4 || latency >= 900) {
      return 'danger';
    }

    if (errors >= 2 || latency >= 500) {
      return 'warn';
    }

    return 'success';
  });

  protected readonly apiHealthLabel = computed(() => {
    switch (this.apiHealthSeverity()) {
      case 'danger':
        return 'Degraded';
      case 'warn':
        return 'Watch';
      default:
        return 'Healthy';
    }
  });

  protected readonly incidentRatioLabel = computed(() => {
    const logCount = this.logs().length;
    const errorCount = this.errorLogs().length;

    if (!logCount) {
      return '0%';
    }

    return `${Math.round((errorCount / logCount) * 100)}%`;
  });

  protected readonly healthHint = computed(() => {
    const latency = this.averageLatency();

    if (!latency) {
      return 'Awaiting traffic sample';
    }

    return `${latency} ms average across recent requests`;
  });

  constructor() {
    this.loadOverview();
  }

  protected loadOverview(): void {
    this.loading.set(true);

    this.dashboardApi.getOverview(this.buildOverviewQuery()).subscribe({
      next: ({ stats, logs, errorLogs, accessPolicies }) => {
        this.stats.set(stats);
        this.logs.set(logs);
        this.errorLogs.set(errorLogs);
        this.accessPolicies.set(accessPolicies);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
      }
    });
  }

  protected setFromDate(value: string): void {
    this.fromDate.set(value);
    this.loadOverview();
  }

  protected setToDate(value: string): void {
    this.toDate.set(value);
    this.loadOverview();
  }

  protected setQuickRange(days: number): void {
    const now = new Date();
    this.fromDate.set(this.toDateInputValue(this.addDays(now, -days)));
    this.toDate.set(this.toDateInputValue(now));
    this.loadOverview();
  }

  protected resetDateFilter(): void {
    this.fromDate.set('');
    this.toDate.set('');
    this.loadOverview();
  }

  private buildOverviewQuery(): { fromTimestamp?: string; toTimestamp?: string } {
    const from = this.fromDate();
    const to = this.toDate();

    return {
      fromTimestamp: from ? new Date(`${from}T00:00:00`).toISOString() : undefined,
      toTimestamp: to ? new Date(`${to}T23:59:59.999`).toISOString() : undefined
    };
  }

  private fromFilterDate(): Date {
    const from = this.fromDate();

    if (from) {
      return new Date(`${from}T00:00:00`);
    }

    const timestamps = [
      ...this.logs().map((log) => new Date(log.timestamp).getTime()),
      ...this.errorLogs().map((error) => new Date(error.timestamp || error.createdAt).getTime())
    ].filter((value) => Number.isFinite(value));

    return timestamps.length ? new Date(Math.min(...timestamps)) : this.addDays(new Date(), -7);
  }

  private toFilterDate(): Date {
    const to = this.toDate();

    if (to) {
      return new Date(`${to}T23:59:59.999`);
    }

    const timestamps = [
      ...this.logs().map((log) => new Date(log.timestamp).getTime()),
      ...this.errorLogs().map((error) => new Date(error.timestamp || error.createdAt).getTime())
    ].filter((value) => Number.isFinite(value));

    return timestamps.length ? new Date(Math.max(...timestamps)) : new Date();
  }

  private timelineIndex(date: Date, from: Date, bucketSize: number, bucketCount: number): number {
    const rawIndex = Math.floor((date.getTime() - from.getTime()) / bucketSize);

    if (!Number.isFinite(rawIndex)) {
      return 0;
    }

    return Math.min(bucketCount - 1, Math.max(0, rawIndex));
  }

  private formatTimelineLabel(date: Date, span: number): string {
    const day = date.toLocaleDateString(undefined, { month: 'short', day: 'numeric' });

    if (span <= 36 * 60 * 60 * 1000) {
      return date.toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit' });
    }

    return day;
  }

  private addDays(date: Date, days: number): Date {
    const next = new Date(date);
    next.setDate(next.getDate() + days);

    return next;
  }

  private toDateInputValue(date: Date): string {
    const year = date.getFullYear();
    const month = `${date.getMonth() + 1}`.padStart(2, '0');
    const day = `${date.getDate()}`.padStart(2, '0');

    return `${year}-${month}-${day}`;
  }

  protected metricToneClass(tone: DashboardMetric['tone']): string {
    switch (tone) {
      case 'success':
        return 'bg-emerald-50 text-emerald-600';
      case 'warn':
        return 'bg-amber-50 text-amber-600';
      case 'danger':
        return 'bg-rose-50 text-rose-600';
      default:
        return 'bg-blue-50 text-blue-600';
    }
  }

  protected logLevelSeverity(
    level: string
  ): 'success' | 'info' | 'warn' | 'danger' | 'secondary' | 'contrast' {
    switch (level.toLowerCase()) {
      case 'error':
      case 'fatal':
        return 'danger';
      case 'warning':
        return 'warn';
      case 'information':
      case 'info':
        return 'info';
      case 'debug':
      case 'trace':
        return 'secondary';
      default:
        return 'contrast';
    }
  }
}

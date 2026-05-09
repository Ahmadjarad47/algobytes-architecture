import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { finalize, interval, Subscription } from 'rxjs';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { ToggleSwitchModule } from 'primeng/toggleswitch';

import { AuthService } from '../../../../core/services/auth.service';
import { AppToastService } from '../../../../core/services/app-toast.service';
import { AdminConfirmDialog } from '../../../../shared/components/admin-confirm-dialog/admin-confirm-dialog';
import { AdminDataTable } from '../../../../shared/components/admin-data-table/admin-data-table';
import { AdminDetailsDrawer } from '../../../../shared/components/admin-details-drawer/admin-details-drawer';
import {
  AdminBulkAction,
  AdminDetailItem,
  AdminRowAction,
  AdminTableColumn
} from '../../../../shared/models/admin-table.model';
import { exportCsv, exportJson, ExportRow } from '../../../../shared/utils/export.utils';
import { SessionsService } from '../../api/sessions.service';
import {
  ActiveSession,
  ActiveSessionsSummary,
  SessionDeviceType,
  SessionStatus
} from '../../models/active-sessions.models';

type PendingAction = 'revoke-session' | 'revoke-user' | null;

interface ActiveSessionTableRow extends ActiveSession {
  readonly durationLabel: string;
}

interface ActiveSessionFilters {
  search?: string;
  status?: SessionStatus | 'All';
  role?: string;
  device?: SessionDeviceType | 'All';
  browser?: string;
  from?: string;
  to?: string;
  suspiciousOnly?: boolean;
}

@Component({
  selector: 'app-active-sessions-list',
  imports: [
    FormsModule,
    CommonModule,
    ButtonModule,
    DialogModule,
    InputTextModule,
    SelectModule,
    ToggleSwitchModule,
    AdminDataTable,
    AdminDetailsDrawer,
    AdminConfirmDialog
  ],
  template: `
    <div class="dashboard-grid">
      <section class="grid gap-3 md:grid-cols-2 xl:grid-cols-5">
        <article class="surface-card dashboard-section" *ngFor="let card of summaryCards()">
          <div class="flex items-start justify-between gap-2">
            <div>
              <div class="text-[10px] font-semibold uppercase tracking-wide text-slate-500">{{ card.label }}</div>
              <div class="mt-1 text-[20px] font-semibold leading-none text-slate-950">{{ card.value }}</div>
              <div class="mt-1 text-[11px] text-slate-500">{{ card.hint }}</div>
            </div>
            <span class="flex h-8 w-8 items-center justify-center rounded-lg" [class]="card.tone">
              <i [class]="card.icon" class="text-[12px]"></i>
            </span>
          </div>
        </article>
      </section>

      <section class="surface-card dashboard-section">
        <div class="grid gap-3 xl:grid-cols-[minmax(0,1fr)_auto] xl:items-end">
          <div class="grid gap-2 md:grid-cols-3 xl:grid-cols-6">
            <label class="dashboard-filter-field">
              <span>Search</span>
              <input pInputText [(ngModel)]="filters.search" placeholder="User, email, IP" (ngModelChange)="loadSessions()" />
            </label>
            <label class="dashboard-filter-field">
              <span>Status</span>
              <p-select [options]="statusOptions" optionLabel="label" optionValue="value" [(ngModel)]="filters.status" appendTo="body" (ngModelChange)="loadSessions()" />
            </label>
            <label class="dashboard-filter-field">
              <span>Role</span>
              <p-select [options]="roleOptions()" optionLabel="label" optionValue="value" [(ngModel)]="filters.role" appendTo="body" (ngModelChange)="loadSessions()" />
            </label>
            <label class="dashboard-filter-field">
              <span>Device</span>
              <p-select [options]="deviceOptions" optionLabel="label" optionValue="value" [(ngModel)]="filters.device" appendTo="body" (ngModelChange)="loadSessions()" />
            </label>
            <label class="dashboard-filter-field">
              <span>Browser</span>
              <p-select [options]="browserOptions()" optionLabel="label" optionValue="value" [(ngModel)]="filters.browser" appendTo="body" (ngModelChange)="loadSessions()" />
            </label>
            <label class="dashboard-filter-field">
              <span>Auto refresh</span>
              <p-select [options]="refreshOptions" optionLabel="label" optionValue="value" [(ngModel)]="refreshIntervalMs" appendTo="body" (ngModelChange)="setAutoRefresh($event)" />
            </label>
            <label class="dashboard-filter-field">
              <span>From</span>
              <input type="date" [(ngModel)]="filters.from" (ngModelChange)="loadSessions()" />
            </label>
            <label class="dashboard-filter-field">
              <span>To</span>
              <input type="date" [(ngModel)]="filters.to" (ngModelChange)="loadSessions()" />
            </label>
            <label class="settings-switch">
              <span>Suspicious only</span>
              <p-toggleswitch [(ngModel)]="filters.suspiciousOnly" (ngModelChange)="loadSessions()" />
            </label>
          </div>

          <div class="flex flex-wrap gap-2">
            <button type="button" class="dashboard-filter-button" (click)="resetFilters()">Reset</button>
            <button type="button" class="dashboard-filter-button" (click)="loadSessions()">Refresh</button>
            <button type="button" class="dashboard-filter-button is-primary" (click)="openLogoutAllExceptMe()">Logout all except me</button>
          </div>
        </div>
      </section>

      <app-admin-data-table
        title="Active Sessions"
        subtitle="Online users, device context, token expiry, and administrative session controls."
        [columns]="columns"
        [value]="sessions()"
        [loading]="loading()"
        [lazy]="false"
        [rows]="25"
        [totalRecords]="sessions().length"
        [globalFilterFields]="['userName', 'email', 'ipAddress']"
        [showCreate]="false"
        [selectable]="permissions().revoke"
        [bulkActions]="bulkActions()"
        [showExport]="permissions().export"
        [actions]="actions()"
        [horizontalScroll]="true"
        tableMinWidth="1320px"
        searchPlaceholder="Search sessions"
        emptyTitle="No sessions found"
        emptyMessage="Adjust filters or refresh to inspect active user sessions."
        (refresh)="loadSessions()"
        (rowAction)="handleAction($event.actionId, $event.row)"
        (bulkAction)="handleBulkAction($event.actionId, $event.rows)"
        (exportCsv)="exportRows('active-sessions', $event)"
        (exportJson)="exportRowsJson('active-sessions', $event)"
      />

      <app-admin-details-drawer
        [visible]="detailsVisible()"
        [title]="selectedSession()?.userName ?? 'Session details'"
        [items]="detailItems()"
        [showCopy]="true"
        copyLabel="Copy session ID"
        secondaryCopyLabel="Copy IP address"
        actionLabel="Force logout session"
        actionIcon="pi pi-sign-out"
        secondaryActionLabel="Logout all sessions"
        secondaryActionIcon="pi pi-users"
        (visibleChange)="detailsVisible.set($event)"
        (copy)="copySessionId()"
        (secondaryCopy)="copyIpAddress()"
        (action)="selectedSession() && openRevokeSession(selectedSession()!)"
        (secondaryAction)="selectedSession() && openRevokeUser(selectedSession()!)"
      />

      <app-admin-confirm-dialog
        [visible]="confirmVisible()"
        [title]="confirmTitle()"
        [message]="confirmMessage()"
        [description]="confirmDescription()"
        confirmLabel="Force logout"
        [loading]="revoking()"
        (visibleChange)="closeConfirm($event)"
        (confirm)="confirmDangerousAction()"
      />

      <p-dialog
        [visible]="logoutAllExceptMeVisible()"
        header="Logout all users except me"
        [modal]="true"
        [draggable]="false"
        [resizable]="false"
        [style]="{ width: 'min(32rem, 92vw)' }"
        styleClass="surface-dialog"
        (visibleChange)="logoutAllExceptMeVisible.set($event)"
      >
        <div class="grid gap-3">
          <p class="m-0 text-sm font-semibold text-surface-950">This will revoke every mock session except the current admin session.</p>
          <p class="m-0 text-xs leading-5 text-surface-500">Type LOGOUT to confirm this enterprise-wide action.</p>
          <input pInputText [(ngModel)]="logoutConfirmationText" placeholder="LOGOUT" />
          <div class="flex justify-end gap-2 border-t border-surface-200 pt-3">
            <p-button label="Cancel" severity="secondary" size="small" [outlined]="true" (onClick)="logoutAllExceptMeVisible.set(false)" />
            <p-button label="Logout all except me" severity="danger" size="small" [disabled]="logoutConfirmationText !== 'LOGOUT'" [loading]="revoking()" (onClick)="confirmLogoutAllExceptMe()" />
          </div>
        </div>
      </p-dialog>
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ActiveSessionsList {
  private readonly sessionsService = inject(SessionsService);
  private readonly toast = inject(AppToastService);
  private readonly auth = inject(AuthService);
  private autoRefreshSubscription?: Subscription;

  protected readonly permissions = computed(() => this.sessionsService.permissions);
  protected readonly sessions = signal<ActiveSessionTableRow[]>([]);
  protected readonly allSessions = signal<ActiveSession[]>([]);
  protected readonly summary = signal<ActiveSessionsSummary>({
    onlineUsers: 0,
    idleUsers: 0,
    activeSessions: 0,
    suspiciousSessions: 0,
    revokedToday: 0
  });
  protected readonly loading = signal(false);
  protected readonly revoking = signal(false);
  protected readonly detailsVisible = signal(false);
  protected readonly selectedSession = signal<ActiveSession | null>(null);
  protected readonly confirmVisible = signal(false);
  protected readonly pendingAction = signal<PendingAction>(null);
  protected readonly pendingSession = signal<ActiveSession | null>(null);
  protected readonly logoutAllExceptMeVisible = signal(false);
  protected logoutConfirmationText = '';
  protected refreshIntervalMs = 0;

  protected readonly filters: ActiveSessionFilters = {
    status: 'All',
    role: 'All',
    device: 'All',
    browser: 'All',
    suspiciousOnly: false
  };

  protected readonly statusOptions = ['All', 'Online', 'Idle', 'Offline', 'Expired', 'Revoked'].map((value) => ({ label: value, value }));
  protected readonly deviceOptions = ['All', 'Desktop', 'Laptop', 'Tablet', 'Mobile'].map((value) => ({ label: value, value }));
  protected readonly refreshOptions = [
    { label: 'Off', value: 0 },
    { label: '10 seconds', value: 10_000 },
    { label: '30 seconds', value: 30_000 },
    { label: '1 minute', value: 60_000 }
  ];

  protected readonly columns: AdminTableColumn[] = [
    { field: 'userName', header: 'User', sortable: true },
    { field: 'email', header: 'Email' },
    { field: 'role', header: 'Role', filter: true },
    {
      field: 'status',
      header: 'Status',
      cellType: 'status',
      severityMap: {
        Online: 'success',
        Idle: 'warn',
        Offline: 'secondary',
        Expired: 'contrast',
        Revoked: 'danger'
      }
    },
    { field: 'device', header: 'Device' },
    { field: 'browser', header: 'Browser' },
    { field: 'os', header: 'OS' },
    { field: 'ipAddress', header: 'IP address' },
    { field: 'location', header: 'Location' },
    { field: 'loginTime', header: 'Login time', cellType: 'date', sortable: true },
    { field: 'lastActivity', header: 'Last activity', cellType: 'date', sortable: true },
    { field: 'durationLabel', header: 'Session duration' },
    { field: 'expiresAt', header: 'Expires at', cellType: 'date' }
  ];

  protected readonly detailItems = computed<AdminDetailItem[]>(() => {
    const session = this.selectedSession();
    if (!session) {
      return [];
    }

    return [
      { label: 'User name', value: session.userName },
      { label: 'Email', value: session.email },
      { label: 'Role', value: session.role },
      { label: 'Session ID', value: session.id },
      { label: 'Status', value: session.status, type: 'status', severity: this.statusSeverity(session.status) },
      { label: 'Device', value: session.device },
      { label: 'Browser', value: session.browser },
      { label: 'Operating system', value: session.os },
      { label: 'IP address', value: session.ipAddress },
      { label: 'Location', value: session.location },
      { label: 'Login time', value: session.loginTime, type: 'date' },
      { label: 'Last activity time', value: session.lastActivity, type: 'date' },
      { label: 'Session duration', value: this.durationLabel(session.durationMinutes) },
      { label: 'Token expires at', value: session.expiresAt, type: 'date' },
      { label: 'Refresh token expires at placeholder', value: session.refreshTokenExpiresAt, type: 'date' },
      { label: 'Is current admin session', value: session.currentAdminSession ? 'Yes' : 'No', type: 'status', severity: session.currentAdminSession ? 'warn' : 'secondary' },
      { label: 'Is trusted device placeholder', value: session.trustedDevice ? 'Trusted' : 'Untrusted', type: 'status', severity: session.trustedDevice ? 'success' : 'warn' },
      { label: 'Revoked at', value: session.revokedAt, type: 'date' },
      { label: 'Revoked by', value: session.revokedBy },
      { label: 'User agent', value: session.userAgent },
      { label: 'Recent activity timeline placeholder', value: session.activityTimeline, type: 'list' }
    ];
  });

  protected readonly summaryCards = computed(() => {
    const summary = this.summary();

    return [
      { label: 'Online users', value: summary.onlineUsers, hint: 'Currently active', icon: 'pi pi-wifi', tone: 'bg-emerald-50 text-emerald-600' },
      { label: 'Idle users', value: summary.idleUsers, hint: 'No recent activity', icon: 'pi pi-clock', tone: 'bg-amber-50 text-amber-600' },
      { label: 'Active sessions', value: summary.activeSessions, hint: 'Online or idle', icon: 'pi pi-desktop', tone: 'bg-blue-50 text-blue-600' },
      { label: 'Suspicious sessions', value: summary.suspiciousSessions, hint: 'Flagged examples', icon: 'pi pi-shield', tone: 'bg-rose-50 text-rose-600' },
      { label: 'Revoked today', value: summary.revokedToday, hint: 'Audit placeholders', icon: 'pi pi-ban', tone: 'bg-slate-100 text-slate-600' }
    ];
  });

  protected readonly roleOptions = computed(() => toOptions(['All', ...new Set(this.allSessions().map((session) => session.role))]));
  protected readonly browserOptions = computed(() => toOptions(['All', ...new Set(this.allSessions().map((session) => session.browser))]));

  protected readonly actions = computed<AdminRowAction<ActiveSession>[]>(() => [
    { id: 'view', label: 'View session details', icon: 'pi pi-eye', disabled: () => !this.permissions().view },
    { id: 'revoke-session', label: 'Force logout this session', icon: 'pi pi-sign-out', severity: 'danger', disabled: (row) => !this.permissions().revoke || row.status === 'Revoked' },
    { id: 'revoke-user', label: 'Logout all sessions for this user', icon: 'pi pi-users', severity: 'warn', disabled: () => !this.permissions().revokeAll },
    { id: 'lock-user', label: 'Lock user placeholder', icon: 'pi pi-lock', severity: 'warn' },
    { id: 'password-reset', label: 'Require password reset placeholder', icon: 'pi pi-key' }
  ]);

  protected readonly bulkActions = computed<AdminBulkAction[]>(() => [
    { id: 'revoke-selected', label: 'Force logout selected', icon: 'pi pi-sign-out', severity: 'danger' },
    { id: 'export-selected', label: 'Export selected', icon: 'pi pi-download' },
    { id: 'mark-suspicious', label: 'Mark suspicious', icon: 'pi pi-flag', severity: 'warn' }
  ]);

  protected readonly confirmTitle = computed(() => this.pendingAction() === 'revoke-user' ? 'Logout all sessions for user' : 'Force logout session');
  protected readonly confirmMessage = computed(() => {
    const session = this.pendingSession();
    if (!session) {
      return 'Confirm session action?';
    }

    return this.pendingAction() === 'revoke-user'
      ? `Logout all sessions for ${session.email}?`
      : `Force logout ${session.email} on ${session.device}?`;
  });
  protected readonly confirmDescription = computed(() => {
    const session = this.pendingSession();
    if (!session) {
      return 'This will revoke active tokens in the mock session store.';
    }

    const warning = session.currentAdminSession
      ? ' Strong warning: this targets the current admin account/session.'
      : '';

    return `This revokes active tokens and writes an audit event placeholder.${warning}`;
  });

  constructor() {
    this.loadSessions();
  }

  protected loadSessions(): void {
    this.loading.set(true);

    this.sessionsService
      .getSessions(this.filters)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (sessions) => {
          this.sessions.set(sessions.map((session) => ({
            ...session,
            durationLabel: this.durationLabel(session.durationMinutes)
          })));
          this.sessionsService.getSessions().subscribe((allSessions) => this.allSessions.set(allSessions));
          this.sessionsService.getSummary(this.filters).subscribe((summary) => this.summary.set(summary));
        },
        error: () => this.toast.error('Failed to load sessions', 'Mock sessions service returned an error.')
      });
  }

  protected resetFilters(): void {
    this.filters.search = '';
    this.filters.status = 'All';
    this.filters.role = 'All';
    this.filters.device = 'All';
    this.filters.browser = 'All';
    this.filters.from = undefined;
    this.filters.to = undefined;
    this.filters.suspiciousOnly = false;
    this.loadSessions();
  }

  protected setAutoRefresh(value: number): void {
    this.autoRefreshSubscription?.unsubscribe();
    this.refreshIntervalMs = value;

    if (value > 0) {
      this.autoRefreshSubscription = interval(value).subscribe(() => this.loadSessions());
    }
  }

  protected handleAction(actionId: string, row: ActiveSession): void {
    switch (actionId) {
      case 'view':
        this.sessionsService.getSession(row.id).subscribe((session) => {
          this.selectedSession.set(session);
          this.detailsVisible.set(true);
        });
        break;
      case 'revoke-session':
        this.openRevokeSession(row);
        break;
      case 'revoke-user':
        this.openRevokeUser(row);
        break;
      case 'lock-user':
        this.toast.info('Lock user placeholder', `Lock flow for ${row.email} can be connected to Users API.`);
        break;
      case 'password-reset':
        this.toast.info('Password reset required', `Placeholder for ${row.email}.`);
        break;
    }
  }

  protected handleBulkAction(actionId: string, rows: ActiveSessionTableRow[]): void {
    if (rows.length === 0) {
      return;
    }

    if (actionId === 'export-selected') {
      this.exportRows('selected-active-sessions', rows);
      return;
    }

    if (actionId === 'mark-suspicious') {
      this.toast.warn('Marked suspicious placeholder', `${rows.length} sessions selected.`);
      return;
    }

    this.revoking.set(true);
    this.sessionsService
      .revokeSelectedSessions(rows.map((row) => row.id), this.actorName())
      .pipe(finalize(() => this.revoking.set(false)))
      .subscribe({
        next: () => {
          this.toast.success('Session revoked successfully', `${rows.length} sessions were revoked.`);
          this.loadSessions();
        },
        error: () => this.toast.error('Failed to revoke session')
      });
  }

  protected openRevokeSession(session: ActiveSession): void {
    if (session.currentAdminSession) {
      this.toast.warn('You cannot revoke your current session without confirmation', 'Review the warning before confirming.');
    }
    this.pendingAction.set('revoke-session');
    this.pendingSession.set(session);
    this.confirmVisible.set(true);
  }

  protected openRevokeUser(session: ActiveSession): void {
    this.pendingAction.set('revoke-user');
    this.pendingSession.set(session);
    this.confirmVisible.set(true);
  }

  protected closeConfirm(visible: boolean): void {
    this.confirmVisible.set(visible);
    if (!visible && !this.revoking()) {
      this.pendingAction.set(null);
      this.pendingSession.set(null);
    }
  }

  protected confirmDangerousAction(): void {
    const session = this.pendingSession();
    if (!session) {
      return;
    }

    const request = this.pendingAction() === 'revoke-user'
      ? this.sessionsService.revokeUserSessions(session.userId, this.actorName(), session.currentAdminSession)
      : this.sessionsService.revokeSession(session.id, this.actorName(), session.currentAdminSession);

    this.revoking.set(true);
    request.pipe(finalize(() => this.revoking.set(false))).subscribe({
      next: () => {
        this.toast.success(
          this.pendingAction() === 'revoke-user'
            ? 'User sessions revoked successfully'
            : 'Session revoked successfully',
          session.email
        );
        this.confirmVisible.set(false);
        this.detailsVisible.set(false);
        this.pendingAction.set(null);
        this.pendingSession.set(null);
        this.loadSessions();
      },
      error: () => this.toast.error('Failed to revoke session')
    });
  }

  protected openLogoutAllExceptMe(): void {
    this.logoutConfirmationText = '';
    this.logoutAllExceptMeVisible.set(true);
  }

  protected confirmLogoutAllExceptMe(): void {
    if (this.logoutConfirmationText !== 'LOGOUT') {
      return;
    }

    this.revoking.set(true);
    this.sessionsService
      .revokeAllExceptCurrent(this.actorName(), this.logoutConfirmationText)
      .pipe(finalize(() => this.revoking.set(false)))
      .subscribe(() => {
        this.toast.success('User sessions revoked successfully', 'All mock sessions except yours were revoked.');
        this.logoutAllExceptMeVisible.set(false);
        this.loadSessions();
      });
  }

  protected copySessionId(): void {
    const session = this.selectedSession();
    if (!session) {
      return;
    }

    void navigator.clipboard?.writeText(session.id);
    this.toast.success('Copied', 'Session ID copied to clipboard.');
  }

  protected copyIpAddress(): void {
    const session = this.selectedSession();
    if (!session) {
      return;
    }

    void navigator.clipboard?.writeText(session.ipAddress);
    this.toast.success('Copied', 'IP address copied to clipboard.');
  }

  protected exportRows(fileName: string, rows: ActiveSessionTableRow[]): void {
    exportCsv(fileName, rows as unknown as ExportRow[]);
  }

  protected exportRowsJson(fileName: string, rows: ActiveSessionTableRow[]): void {
    exportJson(fileName, rows as unknown as ExportRow[]);
  }

  private actorName(): string {
    return this.auth.session()?.user?.email ?? 'Current admin';
  }

  private durationLabel(minutes: number): string {
    if (minutes < 60) {
      return `${minutes} min`;
    }

    const hours = Math.floor(minutes / 60);
    const remaining = minutes % 60;

    return remaining ? `${hours}h ${remaining}m` : `${hours}h`;
  }

  private statusSeverity(status: SessionStatus): AdminDetailItem['severity'] {
    const severities: Record<SessionStatus, NonNullable<AdminDetailItem['severity']>> = {
      Online: 'success',
      Idle: 'warn',
      Offline: 'secondary',
      Expired: 'contrast',
      Revoked: 'danger'
    };

    return severities[status];
  }
}

function toOptions(values: readonly string[]): { label: string; value: string }[] {
  return values.map((value) => ({ label: value, value }));
}

import { ChangeDetectionStrategy, Component, computed, effect, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { finalize } from 'rxjs';
import { TableLazyLoadEvent } from 'primeng/table';
import dagre from 'dagre';

import { AdminConfirmDialog } from '../../../../shared/components/admin-confirm-dialog/admin-confirm-dialog';
import { AdminDataTable } from '../../../../shared/components/admin-data-table/admin-data-table';
import { AdminDetailsDrawer } from '../../../../shared/components/admin-details-drawer/admin-details-drawer';
import { AdminFormDialog } from '../../../../shared/components/admin-form-dialog/admin-form-dialog';
import {
  AdminDetailItem,
  AdminBulkAction,
  AdminFormField,
  AdminRowAction,
  AdminTableColumn
} from '../../../../shared/models/admin-table.model';
import { toTableQuery } from '../../../../shared/utils/admin-table.utils';
import { AppToastService } from '../../../../core/services/app-toast.service';
import { AdminActionBusService } from '../../../../core/services/admin-action-bus.service';
import { Permissions } from '../../../../core/permissions/permission.catalog';
import { PermissionService } from '../../../../core/permissions/permission.service';
import { SessionRealtimeService } from '../../../../core/services/session-realtime.service';
import { downloadCsvTemplate, exportCsv, exportJson, ExportRow } from '../../../../shared/utils/export.utils';
import { UsersApiService } from '../../api/users-api.service';
import {
  CreateUserCommand,
  UpdateUserRequest,
  UserDetails,
  UserListItem,
  UserPermissionGraph
} from '../../models/users.models';
import { RolesApiService } from '../../../roles/api/roles-api.service';
import { RoleDto } from '../../../roles/models/roles.models';

@Component({
  selector: 'app-users-list',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    RouterLinkActive,
    AdminDataTable,
    AdminFormDialog,
    AdminDetailsDrawer,
    AdminConfirmDialog
  ],
  template: `
    <section class="surface-card dashboard-section mb-3">
      <div class="flex flex-wrap items-center gap-2">
        <a
          routerLink="/users/directory"
          routerLinkActive="bg-slate-900 text-white"
          class="rounded-full px-3 py-1.5 text-xs font-semibold text-slate-600 transition hover:bg-slate-100"
        >
          Directory
        </a>
        <a
          routerLink="/users/chat"
          routerLinkActive="bg-slate-900 text-white"
          class="rounded-full px-3 py-1.5 text-xs font-semibold text-slate-600 transition hover:bg-slate-100"
        >
          Chat
        </a>
      </div>
    </section>

    <app-admin-data-table
      title="Users"
      subtitle="Directory management with server-side search, filters, sorting, and paging."
      [columns]="columns"
      [value]="users()"
      [loading]="loading()"
      [lazy]="true"
      [rows]="pageSize()"
      [first]="first()"
      [totalRecords]="totalRecords()"
      [globalFilterFields]="['displayName', 'email', 'userName']"
      [selectable]="true"
      [showCreate]="canCreate()"
      [bulkActions]="bulkActions()"
      [showExport]="canExport()"
      searchPlaceholder="Search users"
      emptyTitle="No users found"
      emptyMessage="Try a different filter combination or add a new user."
      [actions]="actions()"
      (lazyLoad)="loadUsers($event)"
      (refresh)="reload()"
      (create)="openCreate()"
      (rowAction)="handleAction($event.actionId, $event.row)"
      (bulkAction)="handleBulkAction($event.actionId, $event.rows)"
      (exportCsv)="exportRows('users', $event)"
      (exportJson)="exportRowsJson('users', $event)"
    />

    <section class="surface-card dashboard-section mt-3">
      <div class="flex flex-col gap-2 md:flex-row md:items-center md:justify-between">
        <div>
          <div class="text-[11px] font-semibold uppercase tracking-wide text-slate-500">Import users</div>
          <div class="mt-1 text-sm font-semibold text-slate-950">CSV import placeholder</div>
        </div>
        <div class="flex flex-wrap gap-2">
          <label class="dashboard-filter-button flex cursor-pointer items-center gap-2">
            <i class="pi pi-upload text-[11px]"></i>
            Import users CSV
            <input type="file" accept=".csv" class="hidden" (change)="importUsersPlaceholder()" />
          </label>
          <button type="button" class="dashboard-filter-button" (click)="downloadTemplate()">
            Download CSV template
          </button>
        </div>
      </div>
    </section>

    <app-admin-form-dialog
      [visible]="formVisible()"
      [title]="editingUserId() ? 'Edit user' : 'Create user'"
      [form]="form"
      [fields]="formFields()"
      [submitLabel]="editingUserId() ? 'Save changes' : 'Create user'"
      [loading]="saving()"
      (visibleChange)="closeForm($event)"
      (submit)="save()"
    />

    <app-admin-form-dialog
      [visible]="assignRolesVisible()"
      title="Assign roles"
      [form]="assignRolesForm"
      [fields]="assignRoleFields()"
      submitLabel="Assign roles"
      [loading]="assigningRoles()"
      (visibleChange)="closeAssignRoles($event)"
      (submit)="assignRoles()"
    />

    <app-admin-details-drawer
      [visible]="detailsVisible()"
      [title]="selectedUser()?.displayName ?? 'User details'"
      [items]="detailItems()"
      (visibleChange)="detailsVisible.set($event)"
    />

    @if (graphVisible()) {
      <div class="permission-graph-overlay" (click)="closeGraph()">
        <div class="permission-graph-modal" (click)="$event.stopPropagation()">
          <div class="permission-graph-header">
            <h3>Permission Graph - {{ selectedUser()?.displayName ?? 'User' }}</h3>
            <button type="button" class="permission-graph-close" (click)="closeGraph()">
              <i class="pi pi-times"></i>
            </button>
          </div>
          <div
            class="permission-graph-canvas"
            (wheel)="onGraphWheel($event)"
            (pointerdown)="onGraphPointerDown($event)"
            (pointermove)="onGraphPointerMove($event)"
            (pointerup)="onGraphPointerUp()"
            (pointerleave)="onGraphPointerUp()"
          >
            <div class="permission-graph-toolbar" (pointerdown)="$event.stopPropagation()">
              <button type="button" class="permission-graph-tool-btn" (click)="zoomOut()">-</button>
              <button type="button" class="permission-graph-tool-btn" (click)="fitGraph()">Fit</button>
              <button type="button" class="permission-graph-tool-btn" (click)="zoomIn()">+</button>
            </div>
            <svg
              [attr.viewBox]="graphViewBox()"
              [attr.width]="graphViewportSize().width"
              [attr.height]="graphViewportSize().height"
              preserveAspectRatio="xMidYMid meet"
              class="permission-graph-svg"
            >
              <defs>
                <marker id="permission-graph-arrow" viewBox="0 0 10 10" refX="8" refY="5" markerWidth="6" markerHeight="6" orient="auto-start-reverse">
                  <path d="M 0 0 L 10 5 L 0 10 z" class="permission-graph-arrow" />
                </marker>
              </defs>
              <g [attr.transform]="graphTransform()">
                @for (edge of graphEdges(); track edge.id) {
                  <path [attr.d]="edge.path" class="permission-graph-edge" />
                }
                @for (node of graphNodes(); track node.id) {
                  <g [attr.transform]="'translate(' + node.x + ' ' + node.y + ')'" class="permission-graph-node-group">
                    <rect
                      [attr.x]="-node.width / 2"
                      [attr.y]="-node.height / 2"
                      [attr.width]="node.width"
                      [attr.height]="node.height"
                      rx="14"
                      [attr.class]="'permission-graph-node permission-graph-node-' + node.kind"
                    />
                    <text x="0" y="-5" class="permission-graph-label" text-anchor="middle">{{ node.label }}</text>
                    <text x="0" y="17" class="permission-graph-type-label" text-anchor="middle">{{ node.typeLabel }}</text>
                  </g>
                }
              </g>
            </svg>
          </div>
        </div>
      </div>
    }

    <app-admin-confirm-dialog
      [visible]="deleteDialogVisible()"
      title="Delete user"
      [message]="'Delete ' + (pendingDeleteUser()?.displayName ?? 'this user') + '?'"
      description="The user account will be removed from the directory. This action cannot be undone."
      confirmLabel="Delete user"
      [loading]="deleting()"
      (visibleChange)="closeDeleteDialog($event)"
      (confirm)="confirmDelete()"
    />
  `,
  styles: [`
    .permission-graph-overlay { position: fixed; inset: 0; z-index: 1300; background: rgba(2,8,23,.82); backdrop-filter: blur(4px); display:flex; align-items:center; justify-content:center; padding:1rem; }
    .permission-graph-modal { width:min(1200px,98vw); height:min(780px,94vh); background:#15171b; border:1px solid #343a43; border-radius:16px; box-shadow:0 30px 80px rgba(2,6,23,.65); display:flex; flex-direction:column; overflow:hidden; }
    .permission-graph-header { display:flex; align-items:center; justify-content:space-between; padding:1rem 1.25rem; color:#e5e7eb; border-bottom:1px solid #243043; }
    .permission-graph-header h3 { margin:0; font-size:1.2rem; font-weight:600; }
    .permission-graph-close { border:0; background:transparent; color:#94a3b8; cursor:pointer; font-size:1rem; }
    .permission-graph-close:hover { color:#e2e8f0; }
    .permission-graph-canvas { position:relative; flex:1; padding:.75rem; overflow:hidden; cursor:grab; touch-action:none; background-color:#101114; background-image:radial-gradient(circle at 1px 1px, rgba(148,163,184,.22) 1px, transparent 0); background-size:24px 24px; }
    .permission-graph-canvas:active { cursor:grabbing; }
    .permission-graph-toolbar { position:absolute; top:1rem; right:1rem; z-index:2; display:flex; gap:.4rem; }
    .permission-graph-tool-btn { border:1px solid #334155; background:#0f172a; color:#e2e8f0; border-radius:10px; padding:.35rem .65rem; font-size:.82rem; cursor:pointer; }
    .permission-graph-tool-btn:hover { border-color:#38bdf8; color:#f8fafc; }
    .permission-graph-svg { width:100%; height:100%; display:block; }
    .permission-graph-edge { fill:none; stroke:#8aa4bf; stroke-width:2.4; opacity:.95; marker-end:url(#permission-graph-arrow); transition:d .35s ease; }
    .permission-graph-arrow { fill:#8aa4bf; }
    .permission-graph-node-group { transition:transform .35s ease; }
    .permission-graph-node { fill:#242932; stroke:#6b7280; stroke-width:2.4; }
    .permission-graph-node-user { fill:#0f8f82; stroke:#76f1dc; stroke-width:3; }
    .permission-graph-node-role { fill:#193d6b; stroke:#83b8ff; }
    .permission-graph-node-policy { fill:#145348; stroke:#5eead4; }
    .permission-graph-node-resource { fill:#4a2a72; stroke:#d8b4fe; }
    .permission-graph-label { fill:#ffffff; font-size:15px; font-weight:800; dominant-baseline:middle; pointer-events:none; }
    .permission-graph-type-label { fill:#d5dbe5; font-size:11px; font-weight:800; letter-spacing:.08em; text-transform:uppercase; dominant-baseline:middle; pointer-events:none; }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class UsersList {
  private readonly api = inject(UsersApiService);
  private readonly rolesApi = inject(RolesApiService);
  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly toast = inject(AppToastService);
  private readonly actionBus = inject(AdminActionBusService);
  private readonly permissionService = inject(PermissionService);
  private readonly sessionRealtime = inject(SessionRealtimeService);

  protected readonly users = signal<UserListItem[]>([]);
  protected readonly loading = signal(false);
  protected readonly saving = signal(false);
  protected readonly pageSize = signal(25);
  protected readonly first = signal(0);
  protected readonly totalRecords = signal(0);
  protected readonly formVisible = signal(false);
  protected readonly detailsVisible = signal(false);
  protected readonly graphVisible = signal(false);
  protected readonly editingUserId = signal<string | null>(null);
  protected readonly selectedUser = signal<UserDetails | null>(null);
  protected readonly selectedGraph = signal<UserPermissionGraph | null>(null);
  protected readonly graphScale = signal(1);
  protected readonly graphPanX = signal(0);
  protected readonly graphPanY = signal(0);
  protected readonly graphIsPanning = signal(false);
  private panPointerId: number | null = null;
  private panStartClientX = 0;
  private panStartClientY = 0;
  private panStartX = 0;
  private panStartY = 0;
  protected readonly deleteDialogVisible = signal(false);
  protected readonly deleting = signal(false);
  protected readonly pendingDeleteUser = signal<UserListItem | null>(null);
  protected readonly assignRolesVisible = signal(false);
  protected readonly assigningRoles = signal(false);
  protected readonly pendingAssignUser = signal<UserListItem | null>(null);
  protected readonly roles = signal<RoleDto[]>([]);
  protected readonly canCreate = computed(() => this.permissionService.can({ any: [Permissions.users.create] }));
  protected readonly canExport = computed(() => this.permissionService.can({ any: [Permissions.users.read] }));

  protected readonly columns: AdminTableColumn[] = [
    { field: 'displayName', header: 'Display name', sortable: true },
    { field: 'email', header: 'Email' },
    { field: 'userName', header: 'Username' },
    {
      field: 'isActive',
      header: 'Active',
      sortable: true,
      filter: true,
      filterType: 'boolean',
      cellType: 'boolean'
    },
    {
      field: 'isLocked',
      header: 'Locked',
      filter: true,
      filterType: 'boolean',
      cellType: 'boolean'
    },
    {
      field: 'emailConfirmed',
      header: 'Email confirmed',
      filter: true,
      filterType: 'boolean',
      cellType: 'boolean'
    },
    {
      field: 'lastLoginAt',
      header: 'Last login',
      sortable: true,
      cellType: 'date'
    },
    {
      field: 'isOnline',
      header: 'Online',
      cellType: 'boolean'
    },
    {
      field: 'roles',
      header: 'Roles',
      cellType: 'list'
    },
    {
      field: 'twoFactorEnabled',
      header: '2FA',
      cellType: 'boolean'
    }
  ];

  protected readonly actions = computed<AdminRowAction<UserListItem>[]>(() => {
    const canUpdate = this.permissionService.can({ any: [Permissions.users.update] });
    const canDelete = this.permissionService.can({ any: [Permissions.users.delete] });
    const canReadRoles = this.permissionService.can({ any: [Permissions.roles.read] });

    return [
      { id: 'view', label: 'View details', icon: 'pi pi-eye' },
      { id: 'permission-graph', label: 'Permission graph', icon: 'pi pi-sitemap' },
      ...(canUpdate ? [
        { id: 'edit', label: 'Edit user', icon: 'pi pi-pencil' },
        ...(canReadRoles ? [{ id: 'assign-roles', label: 'Assign roles', icon: 'pi pi-user-plus' } as AdminRowAction<UserListItem>] : []),
        { id: 'toggle-totp', label: 'Require or disable 2FA', icon: 'pi pi-mobile', severity: 'warn' as const },
        { id: 'toggle-active', label: 'Toggle active', icon: 'pi pi-power-off', severity: 'warn' as const },
        { id: 'toggle-lock', label: 'Lock or unlock', icon: 'pi pi-lock', severity: 'warn' as const },
        { id: 'reset-password', label: 'Reset password', icon: 'pi pi-key' }
      ] : []),
      ...(canDelete ? [{ id: 'delete', label: 'Delete user', icon: 'pi pi-trash', severity: 'danger' as const }] : [])
    ];
  });

  protected readonly bulkActions = computed<AdminBulkAction[]>(() => {
    const canUpdate = this.permissionService.can({ any: [Permissions.users.update] });
    const canDelete = this.permissionService.can({ any: [Permissions.users.delete] });
    const canExport = this.permissionService.can({ any: [Permissions.users.read] });

    return [
      ...(canUpdate ? [
        { id: 'activate', label: 'Activate', icon: 'pi pi-check', severity: 'success' as const },
        { id: 'deactivate', label: 'Deactivate', icon: 'pi pi-ban', severity: 'warn' as const },
        { id: 'lock', label: 'Lock', icon: 'pi pi-lock', severity: 'warn' as const },
        { id: 'unlock', label: 'Unlock', icon: 'pi pi-lock-open', severity: 'success' as const },
        { id: 'assign-role', label: 'Assign role', icon: 'pi pi-user-plus' }
      ] : []),
      ...(canDelete ? [{ id: 'delete', label: 'Delete', icon: 'pi pi-trash', severity: 'danger' as const }] : []),
      ...(canExport ? [{ id: 'export-selected', label: 'Export selected', icon: 'pi pi-download' }] : [])
    ];
  });

  protected readonly form = this.formBuilder.group({
    email: ['', [Validators.required, Validators.email]],
    userName: ['', Validators.required],
    displayName: ['', Validators.required],
    phoneNumber: [''],
    password: [''],
    confirmPassword: [''],
    emailConfirmed: [false],
    isActive: [true]
  });

  protected readonly assignRolesForm = this.formBuilder.group({
    roles: [[] as string[], Validators.required]
  });

  protected readonly formFields = computed<AdminFormField[]>(() => {
    const editing = Boolean(this.editingUserId());

    return [
      { key: 'displayName', label: 'Display name', type: 'text' },
      { key: 'email', label: 'Email', type: 'email' },
      { key: 'userName', label: 'Username', type: 'text' },
      { key: 'phoneNumber', label: 'Phone number', type: 'text' },
      ...(editing
        ? []
        : [
            { key: 'password', label: 'Password', type: 'password' } as const,
            { key: 'confirmPassword', label: 'Confirm password', type: 'password' } as const
          ]),
      { key: 'emailConfirmed', label: 'Email confirmed', type: 'switch' },
      { key: 'isActive', label: 'Active', type: 'switch' }
    ];
  });

  protected readonly assignRoleFields = computed<AdminFormField[]>(() => {
    const assigned = new Set(
      (this.pendingAssignUser()?.roles ?? []).map((role) => role.toLowerCase())
    );
    const options = this.roles()
      .filter((role) => !assigned.has(role.name.toLowerCase()))
      .map((role) => ({ label: role.name, value: role.name }));

    return [
      {
        key: 'roles',
        label: options.length ? 'Roles to add' : 'No roles available',
        type: 'multiselect',
        options
      }
    ];
  });

  protected readonly detailItems = computed<AdminDetailItem[]>(() => {
    const user = this.selectedUser();
    if (!user) {
      return [];
    }

    return [
      { label: 'User ID', value: user.userId },
      { label: 'Email', value: user.email },
      { label: 'Username', value: user.userName },
      { label: 'Phone number', value: user.phoneNumber },
      {
        label: 'Active',
        value: user.isActive ? 'Active' : 'Inactive',
        type: 'status',
        severity: user.isActive ? 'success' : 'secondary'
      },
      {
        label: 'Locked',
        value: user.isLocked ? 'Locked' : 'Unlocked',
        type: 'status',
        severity: user.isLocked ? 'warn' : 'success'
      },
      { label: 'Roles', value: user.roles, type: 'list' },
      {
        label: 'Two-factor authentication',
        value: user.twoFactorEnabled ? 'Enabled' : 'Disabled',
        type: 'status',
        severity: user.twoFactorEnabled ? 'success' : 'secondary'
      },
      {
        label: 'Admin policy',
        value: user.totpRequiredByAdmin ? 'Required' : 'Optional',
        type: 'status',
        severity: user.totpRequiredByAdmin ? 'warn' : 'secondary'
      },
      { label: 'Created', value: user.createdAt, type: 'date' },
      { label: 'Last login', value: user.lastLoginAt, type: 'date' }
    ];
  });

  protected readonly graphNodes = computed<GraphNode[]>(() => {
    const graph = this.selectedGraph();
    if (!graph) return [];

    return layoutPermissionGraph(graph);
  });

  protected readonly graphViewBox = computed(() => {
    const size = this.graphViewportSize();
    return `${size.minX} ${size.minY} ${size.width} ${size.height}`;
  });

  protected readonly graphTransform = computed(
    () => `translate(${this.graphPanX()} ${this.graphPanY()}) scale(${this.graphScale()})`
  );

  protected readonly graphViewportSize = computed(() => {
    const nodes = this.graphNodes();
    if (nodes.length === 0) {
      return { minX: 0, minY: 0, width: 1200, height: 760 };
    }

    const margin = 80;
    const minX = Math.min(...nodes.map((node) => node.x - node.width / 2)) - margin;
    const maxX = Math.max(...nodes.map((node) => node.x + node.width / 2)) + margin;
    const minY = Math.min(...nodes.map((node) => node.y - node.height / 2)) - margin;
    const maxY = Math.max(...nodes.map((node) => node.y + node.height / 2)) + margin;

    const width = Math.max(1, maxX - minX);
    const height = Math.max(1, maxY - minY);

    return { minX, minY, width, height };
  });

  protected readonly graphEdges = computed<GraphEdge[]>(() => {
    const graph = this.selectedGraph();
    if (!graph) return [];

    const byId = new Map(this.graphNodes().map((n) => [n.id, n]));
    return graph.edges
      .map((edge, index) => {
        const from = byId.get(edge.from);
        const to = byId.get(edge.to);
        if (!from || !to) return null;
        return { id: `${edge.type}-${index}`, path: buildEdgePath(from, to) };
      })
      .filter((edge): edge is GraphEdge => Boolean(edge));
  });

  private lastLazyEvent: TableLazyLoadEvent = {
    first: 0,
    rows: 25
  };

  constructor() {
    this.sessionRealtime.start();

    effect(() => {
      const onlineUserIds = this.sessionRealtime.onlineUserIds();
      this.users.update((users) => users.map((user) => ({
        ...user,
        isOnline: onlineUserIds.has(user.id)
      })));
    });

    this.actionBus.actions$.subscribe((action) => {
      if (action === 'create-user' && this.canCreate()) {
        this.openCreate();
      }
    });
  }

  protected loadUsers(event: TableLazyLoadEvent): void {
    this.lastLazyEvent = event;
    const query = toTableQuery(event, this.pageSize());

    this.loading.set(true);
    this.pageSize.set(query.pageSize);
    this.first.set((query.pageNumber - 1) * query.pageSize);

    this.api
      .getUsers({
        PageNumber: query.pageNumber,
        PageSize: query.pageSize,
        Search: query.search,
        SortField: query.sortField,
        SortDirection: query.sortDirection,
        IsActive: toBoolean(query.filters?.['isActive']),
        IsLocked: toBoolean(query.filters?.['isLocked']),
        EmailConfirmed: toBoolean(query.filters?.['emailConfirmed'])
      })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (response) => {
          const onlineUserIds = this.sessionRealtime.onlineUserIds();
          this.users.set(response.items.map((user) => ({
            ...user,
            isOnline: user.isOnline || onlineUserIds.has(user.id)
          })));
          this.totalRecords.set(response.totalCount);
        }
      });
  }

  protected reload(): void {
    this.loadUsers(this.lastLazyEvent);
  }

  protected openCreate(): void {
    this.editingUserId.set(null);
    this.form.reset({
      email: '',
      userName: '',
      displayName: '',
      phoneNumber: '',
      password: '',
      confirmPassword: '',
      emailConfirmed: false,
      isActive: true
    });
    this.formVisible.set(true);
  }

  protected closeForm(visible: boolean): void {
    this.formVisible.set(visible);
    if (!visible) {
      this.editingUserId.set(null);
    }
  }

  protected handleAction(actionId: string, row: UserListItem): void {
    switch (actionId) {
      case 'view':
        this.api.getUser(row.id).subscribe((user) => {
          this.selectedUser.set(user);
          this.detailsVisible.set(true);
        });
        break;
      case 'edit':
        this.editingUserId.set(row.id);
        this.form.reset({
          email: row.email ?? '',
          userName: row.userName ?? '',
          displayName: row.displayName,
          phoneNumber: row.phoneNumber ?? '',
          password: '',
          confirmPassword: '',
          emailConfirmed: row.emailConfirmed,
          isActive: row.isActive
        });
        this.formVisible.set(true);
        break;
      case 'permission-graph':
        this.api.getUser(row.id).subscribe((user) => this.selectedUser.set(user));
        this.api.getUserPermissionGraph(row.id).subscribe((graph) => {
          this.selectedGraph.set(graph);
          this.fitGraph();
          this.graphVisible.set(true);
        });
        break;
      case 'assign-roles':
        this.openAssignRoles(row);
        break;
      case 'toggle-active':
        (row.isActive ? this.api.deactivateUser(row.id) : this.api.activateUser(row.id)).subscribe(
          () => {
            const toast = row.isActive ? this.toast.warn.bind(this.toast) : this.toast.success.bind(this.toast);
            toast(
              row.isActive ? 'User deactivated' : 'User activated',
              row.displayName
            );
            this.reload();
          }
        );
        break;
      case 'toggle-totp':
        this.api.setTotpPolicy(row.id, !row.totpRequiredByAdmin).subscribe(() => {
          this.toast.success(
            !row.totpRequiredByAdmin ? '2FA required' : '2FA disabled',
            row.displayName
          );
          this.reload();
        });
        break;
      case 'toggle-lock':
        (row.isLocked ? this.api.unlockUser(row.id) : this.api.lockUser(row.id)).subscribe(() => {
          const toast = row.isLocked ? this.toast.success.bind(this.toast) : this.toast.warn.bind(this.toast);
          toast(row.isLocked ? 'User unlocked' : 'User locked', row.displayName);
          this.reload();
        });
        break;
      case 'reset-password':
        this.toast.info('Reset password placeholder', 'Connect this action to your password reset flow.');
        break;
      case 'delete':
        this.pendingDeleteUser.set(row);
        this.deleteDialogVisible.set(true);
        break;
    }
  }

  protected handleBulkAction(actionId: string, rows: UserListItem[]): void {
    if (rows.length === 0) {
      return;
    }

    if (actionId === 'assign-role') {
      this.toast.info('Bulk assign role', 'Select a role drawer can be wired to your backend rules.');
      return;
    }

    if (actionId === 'delete') {
      this.toast.warn('Bulk delete placeholder', 'Use the row delete confirmation for destructive operations.');
      return;
    }

    if (actionId === 'export-selected') {
      this.exportRows('selected-users', rows);
      return;
    }

    const requests = rows.map((row) => {
      switch (actionId) {
        case 'activate':
          return this.api.activateUser(row.id);
        case 'deactivate':
          return this.api.deactivateUser(row.id);
        case 'lock':
          return this.api.lockUser(row.id);
        case 'unlock':
          return this.api.unlockUser(row.id);
        default:
          return null;
      }
    }).filter(Boolean);

    for (const request of requests) {
      request?.subscribe(() => this.reload());
    }

    this.toast.success('Bulk action queued', `${rows.length} users selected.`);
  }

  protected exportRows(fileName: string, rows: UserListItem[]): void {
    exportCsv(fileName, rows as unknown as ExportRow[]);
  }

  protected exportRowsJson(fileName: string, rows: UserListItem[]): void {
    exportJson(fileName, rows as unknown as ExportRow[]);
  }

  protected importUsersPlaceholder(): void {
    this.toast.info('Import users placeholder', 'CSV parsing can be connected when an import endpoint is available.');
  }

  protected downloadTemplate(): void {
    downloadCsvTemplate('users-template', ['email', 'userName', 'displayName', 'phoneNumber', 'roles']);
  }

  protected closeAssignRoles(visible: boolean): void {
    this.assignRolesVisible.set(visible);
    if (!visible && !this.assigningRoles()) {
      this.pendingAssignUser.set(null);
      this.assignRolesForm.reset({ roles: [] });
    }
  }

  protected assignRoles(): void {
    const user = this.pendingAssignUser();
    const roles = this.assignRolesForm.getRawValue().roles;

    if (!user || this.assignRolesForm.invalid || this.assigningRoles()) {
      return;
    }

    this.assigningRoles.set(true);
    this.api
      .assignRoles(user.id, roles)
      .pipe(finalize(() => this.assigningRoles.set(false)))
      .subscribe(() => {
        this.toast.success('Roles assigned', user.displayName);
        this.assignRolesVisible.set(false);
        this.pendingAssignUser.set(null);
        this.assignRolesForm.reset({ roles: [] });
        this.reload();
      });
  }

  protected closeDeleteDialog(visible: boolean): void {
    this.deleteDialogVisible.set(visible);
    if (!visible && !this.deleting()) {
      this.pendingDeleteUser.set(null);
    }
  }

  protected confirmDelete(): void {
    const user = this.pendingDeleteUser();

    if (!user || this.deleting()) {
      return;
    }

    this.deleting.set(true);
    this.api
      .deleteUser(user.id)
      .pipe(finalize(() => this.deleting.set(false)))
      .subscribe(() => {
        this.toast.danger('User deleted', user.displayName);
        this.deleteDialogVisible.set(false);
        this.pendingDeleteUser.set(null);
        this.reload();
      });
  }

  protected save(): void {
    if (this.form.invalid || this.saving()) {
      return;
    }

    this.saving.set(true);
    const editingId = this.editingUserId();

    const request = editingId
      ? this.toUpdateUserRequest()
      : this.toCreateUserCommand();

    const saveRequest = editingId
      ? this.api.updateUser(editingId, request as UpdateUserRequest)
      : this.api.createUser(request as CreateUserCommand);

    saveRequest
      .pipe(finalize(() => this.saving.set(false)))
      .subscribe(() => {
        this.toast.success(editingId ? 'User updated' : 'User created', this.form.controls.displayName.value);
        this.formVisible.set(false);
        this.reload();
      });
  }

  protected closeGraph(): void {
    this.graphVisible.set(false);
  }

  protected fitGraph(): void {
    this.graphScale.set(1);
    this.graphPanX.set(0);
    this.graphPanY.set(0);
  }

  protected zoomIn(): void {
    const center = this.graphViewportCenter();
    this.setScaleAroundPoint(this.graphScale() * 1.12, center.x, center.y);
  }

  protected zoomOut(): void {
    const center = this.graphViewportCenter();
    this.setScaleAroundPoint(this.graphScale() / 1.12, center.x, center.y);
  }

  protected onGraphWheel(event: WheelEvent): void {
    event.preventDefault();

    const svg = (event.currentTarget as HTMLElement).querySelector('svg');
    if (!svg) return;

    const rect = svg.getBoundingClientRect();
    const v = this.graphViewportSize();
    const worldX = v.minX + ((event.clientX - rect.left) / rect.width) * v.width;
    const worldY = v.minY + ((event.clientY - rect.top) / rect.height) * v.height;
    const factor = event.deltaY < 0 ? 1.08 : 1 / 1.08;
    this.setScaleAroundPoint(this.graphScale() * factor, worldX, worldY);
  }

  protected onGraphPointerDown(event: PointerEvent): void {
    if (event.button !== 0) return;
    this.graphIsPanning.set(true);
    this.panPointerId = event.pointerId;
    this.panStartClientX = event.clientX;
    this.panStartClientY = event.clientY;
    this.panStartX = this.graphPanX();
    this.panStartY = this.graphPanY();
    (event.currentTarget as HTMLElement).setPointerCapture(event.pointerId);
  }

  protected onGraphPointerMove(event: PointerEvent): void {
    if (!this.graphIsPanning() || this.panPointerId !== event.pointerId) return;
    const svg = (event.currentTarget as HTMLElement).querySelector('svg');
    if (!svg) return;

    const rect = svg.getBoundingClientRect();
    const v = this.graphViewportSize();
    const dx = ((event.clientX - this.panStartClientX) / rect.width) * v.width;
    const dy = ((event.clientY - this.panStartClientY) / rect.height) * v.height;
    this.graphPanX.set(this.panStartX + dx);
    this.graphPanY.set(this.panStartY + dy);
  }

  protected onGraphPointerUp(): void {
    this.graphIsPanning.set(false);
    this.panPointerId = null;
  }

  private setScaleAroundPoint(nextScale: number, worldX: number, worldY: number): void {
    const clampedScale = Math.max(0.35, Math.min(2.8, nextScale));
    const currentScale = this.graphScale();
    if (Math.abs(clampedScale - currentScale) < 0.0001) return;

    const panX = this.graphPanX();
    const panY = this.graphPanY();

    const screenX = worldX * currentScale + panX;
    const screenY = worldY * currentScale + panY;
    const nextPanX = screenX - worldX * clampedScale;
    const nextPanY = screenY - worldY * clampedScale;

    this.graphScale.set(clampedScale);
    this.graphPanX.set(nextPanX);
    this.graphPanY.set(nextPanY);
  }

  private graphViewportCenter(): { x: number; y: number } {
    const v = this.graphViewportSize();
    return {
      x: v.minX + v.width / 2,
      y: v.minY + v.height / 2
    };
  }

  private toCreateUserCommand(): CreateUserCommand {
    const value = this.form.getRawValue();

    return {
      email: value.email,
      userName: value.userName,
      displayName: value.displayName,
      phoneNumber: value.phoneNumber || null,
      password: value.password,
      confirmPassword: value.confirmPassword,
      roles: [],
      emailConfirmed: value.emailConfirmed,
      isActive: value.isActive
    };
  }

  private openAssignRoles(user: UserListItem): void {
    this.pendingAssignUser.set(user);
    this.assignRolesForm.reset({ roles: [] });
    this.assignRolesVisible.set(true);

    if (this.roles().length === 0) {
      this.loadRoles();
    }
  }

  private loadRoles(): void {
    this.rolesApi.getRoles().subscribe((roles) => this.roles.set(roles));
  }

  private toUpdateUserRequest(): UpdateUserRequest {
    const value = this.form.getRawValue();

    return {
      displayName: value.displayName,
      userName: value.userName,
      phoneNumber: value.phoneNumber || null,
      isActive: value.isActive,
      emailConfirmed: value.emailConfirmed
    };
  }
}

function toBoolean(value: unknown): boolean | undefined {
  if (value === true || value === false) {
    return value;
  }

  return undefined;
}

type GraphNodeKind = 'user' | 'role' | 'policy' | 'resource';

interface GraphNode {
  id: string;
  label: string;
  kind: GraphNodeKind;
  typeLabel: string;
  x: number;
  y: number;
  width: number;
  height: number;
}

interface GraphEdge {
  id: string;
  path: string;
}

function layoutPermissionGraph(graph: UserPermissionGraph): GraphNode[] {
  const apiUserNode = graph.nodes.find((node) => node.type === 'user');
  const fallbackUserNode = { id: `user:${graph.userId}`, type: 'user', label: 'USER' };
  const sourceNodes = apiUserNode ? [...graph.nodes] : [fallbackUserNode, ...graph.nodes];
  const dagreGraph = new dagre.graphlib.Graph();
  dagreGraph.setGraph({
    rankdir: 'TB',
    align: 'UL',
    nodesep: 72,
    ranksep: 96,
    marginx: 80,
    marginy: 80
  });
  dagreGraph.setDefaultEdgeLabel(() => ({}));

  for (const node of sourceNodes) {
    const size = graphNodeSize(node.type);
    dagreGraph.setNode(node.id, size);
  }

  for (const edge of graph.edges) {
    dagreGraph.setEdge(edge.from, edge.to);
  }

  dagre.layout(dagreGraph);

  const positioned = sourceNodes.map((node) => {
    const layout = dagreGraph.node(node.id) as { x: number; y: number } | undefined;
    const size = graphNodeSize(node.type);

    return {
      id: node.id,
      label: trimLabel(node.label),
      kind: graphNodeKind(node.type),
      typeLabel: graphNodeTypeLabel(node.type),
      x: layout?.x ?? 0,
      y: layout?.y ?? 0,
      width: size.width,
      height: size.height
    };
  });

  const minX = Math.min(...positioned.map((node) => node.x - node.width / 2));
  const maxX = Math.max(...positioned.map((node) => node.x + node.width / 2));
  const minY = Math.min(...positioned.map((node) => node.y - node.height / 2));
  const maxY = Math.max(...positioned.map((node) => node.y + node.height / 2));
  const offsetX = (minX + maxX) / 2;
  const offsetY = (minY + maxY) / 2;

  return positioned.map((node) => ({
    ...node,
    x: node.x - offsetX,
    y: node.y - offsetY
  }));
}

function graphNodeKind(type: string): GraphNodeKind {
  return type === 'user' || type === 'role' || type === 'policy' || type === 'resource' ? type : 'resource';
}

function graphNodeSize(type: string): { width: number; height: number } {
  switch (type) {
    case 'user':
      return { width: 180, height: 72 };
    case 'role':
      return { width: 168, height: 64 };
    case 'policy':
      return { width: 176, height: 64 };
    case 'resource':
      return { width: 154, height: 60 };
    default:
      return { width: 160, height: 60 };
  }
}

function graphNodeTypeLabel(type: string): string {
  switch (type) {
    case 'user':
      return 'Parent user';
    case 'role':
      return 'Role';
    case 'policy':
      return 'Permission';
    case 'resource':
      return 'Resource';
    default:
      return 'Node';
  }
}

function buildEdgePath(from: GraphNode, to: GraphNode): string {
  const startX = from.x;
  const startY = from.y + from.height / 2;
  const endX = to.x;
  const endY = to.y - to.height / 2;
  const midY = startY + (endY - startY) / 2;

  return `M ${startX} ${startY} C ${startX} ${midY}, ${endX} ${midY}, ${endX} ${endY}`;
}

function trimLabel(value: string): string {
  return value.length > 20 ? `${value.slice(0, 19)}...` : value;
}

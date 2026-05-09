import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormsModule, NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import { ButtonModule } from 'primeng/button';
import { ToggleSwitchModule } from 'primeng/toggleswitch';

import { AdminConfirmDialog } from '../../../../shared/components/admin-confirm-dialog/admin-confirm-dialog';
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
import { RolesApiService } from '../../api/roles-api.service';
import { CreateRoleCommand, RoleDetailsDto, RoleDto, UpdateRoleRequest } from '../../models/roles.models';

type PermissionAction = 'View' | 'Create' | 'Edit' | 'Delete';

interface PermissionMatrixRow {
  readonly module: string;
  readonly permissions: Record<PermissionAction, boolean | null>;
}

@Component({
  selector: 'app-roles-list',
  imports: [
    ReactiveFormsModule,
    FormsModule,
    AdminDataTable,
    AdminFormDialog,
    AdminDetailsDrawer,
    AdminConfirmDialog,
    ButtonModule,
    ToggleSwitchModule
  ],
  template: `
    <app-admin-data-table
      title="Roles"
      subtitle="Role catalog with reusable table actions and in-place management."
      [columns]="columns"
      [value]="roles()"
      [loading]="loading()"
      [lazy]="false"
      [rows]="25"
      [totalRecords]="roles().length"
      [globalFilterFields]="['name', 'normalizedName']"
      [showCreate]="canCreate()"
      [showExport]="canExport()"
      searchPlaceholder="Search roles"
      emptyTitle="No roles configured"
      emptyMessage="Create a role to start organizing access."
      [actions]="actions()"
      (refresh)="loadRoles()"
      (create)="openCreate()"
      (rowAction)="handleAction($event.actionId, $event.row)"
      (exportCsv)="exportRows('roles', $event)"
      (exportJson)="exportRowsJson('roles', $event)"
    />

    <section class="surface-card dashboard-section mt-3">
      <div class="mb-3 flex items-center justify-between">
        <div>
          <div class="text-[11px] font-semibold uppercase tracking-wide text-slate-500">Permission matrix</div>
          <h3 class="m-0 mt-1 text-sm font-semibold text-slate-950">Reusable role permissions</h3>
        </div>
        <p-button label="Save matrix" icon="pi pi-check" size="small" severity="secondary" [outlined]="true" (onClick)="saveMatrixPlaceholder()" />
      </div>

      <div class="overflow-x-auto">
        <table class="w-full min-w-[520px] border-separate border-spacing-0 text-left text-[12px]">
          <thead>
            <tr>
              <th class="border-b border-slate-200 px-3 py-2 text-slate-500">Module</th>
              @for (action of permissionActions; track action) {
                <th class="border-b border-slate-200 px-3 py-2 text-center text-slate-500">{{ action }}</th>
              }
            </tr>
          </thead>
          <tbody>
            @for (row of permissionMatrix; track row.module) {
              <tr>
                <td class="border-b border-slate-100 px-3 py-2 font-semibold text-slate-800">{{ row.module }}</td>
                @for (action of permissionActions; track action) {
                  <td class="border-b border-slate-100 px-3 py-2 text-center">
                    @if (row.permissions[action] === null) {
                      <span class="text-slate-300">-</span>
                    } @else {
                      <p-toggleswitch [(ngModel)]="row.permissions[action]" />
                    }
                  </td>
                }
              </tr>
            }
          </tbody>
        </table>
      </div>
    </section>

    <app-admin-form-dialog
      [visible]="formVisible()"
      [title]="editingRoleId() ? 'Edit role' : 'Create role'"
      [form]="form"
      [fields]="fields"
      [submitLabel]="editingRoleId() ? 'Save changes' : 'Create role'"
      [loading]="saving()"
      (visibleChange)="closeForm($event)"
      (submit)="save()"
    />

    <app-admin-details-drawer
      [visible]="detailsVisible()"
      [title]="selectedRole()?.name ?? 'Role details'"
      [items]="detailItems()"
      (visibleChange)="detailsVisible.set($event)"
    />

    <app-admin-confirm-dialog
      [visible]="deleteDialogVisible()"
      title="Delete role"
      [message]="'Delete ' + (pendingDeleteRole()?.name ?? 'this role') + '?'"
      description="Role assignments and authorization behavior may be affected. This action cannot be undone."
      confirmLabel="Delete role"
      [loading]="deleting()"
      (visibleChange)="closeDeleteDialog($event)"
      (confirm)="confirmDelete()"
    />
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class RolesList {
  private readonly api = inject(RolesApiService);
  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly toast = inject(AppToastService);
  private readonly actionBus = inject(AdminActionBusService);
  private readonly permissionService = inject(PermissionService);

  protected readonly roles = signal<RoleDto[]>([]);
  protected readonly loading = signal(false);
  protected readonly saving = signal(false);
  protected readonly formVisible = signal(false);
  protected readonly detailsVisible = signal(false);
  protected readonly editingRoleId = signal<string | null>(null);
  protected readonly selectedRole = signal<RoleDetailsDto | null>(null);
  protected readonly deleteDialogVisible = signal(false);
  protected readonly deleting = signal(false);
  protected readonly pendingDeleteRole = signal<RoleDto | null>(null);
  protected readonly permissionActions: readonly PermissionAction[] = ['View', 'Create', 'Edit', 'Delete'];
  protected readonly permissionMatrix: PermissionMatrixRow[] = [
    { module: 'Users', permissions: { View: true, Create: true, Edit: true, Delete: true } },
    { module: 'Roles', permissions: { View: true, Create: true, Edit: true, Delete: null } },
    { module: 'Access Policies', permissions: { View: true, Create: true, Edit: true, Delete: true } },
    { module: 'Logs', permissions: { View: true, Create: null, Edit: null, Delete: null } },
    { module: 'Settings', permissions: { View: true, Create: null, Edit: true, Delete: null } }
  ];

  protected readonly columns: AdminTableColumn[] = [
    { field: 'name', header: 'Name', sortable: true, filter: true },
    { field: 'normalizedName', header: 'Normalized name', sortable: true }
  ];

  protected readonly canCreate = computed(() => this.permissionService.can({ any: [Permissions.roles.create] }));
  protected readonly canUpdate = computed(() => this.permissionService.can({ any: [Permissions.roles.update] }));
  protected readonly canDelete = computed(() => this.permissionService.can({ any: [Permissions.roles.delete] }));
  protected readonly canExport = computed(() => this.permissionService.can({ any: [Permissions.roles.read] }));

  protected readonly actions = computed<AdminRowAction<RoleDto>[]>(() => [
    { id: 'view', label: 'View role', icon: 'pi pi-eye' },
    ...(this.canUpdate() ? [{ id: 'edit', label: 'Edit role', icon: 'pi pi-pencil' } as AdminRowAction<RoleDto>] : []),
    ...(this.canDelete() ? [{ id: 'delete', label: 'Delete role', icon: 'pi pi-trash', severity: 'danger' as const }] : [])
  ]);

  protected readonly fields: AdminFormField[] = [
    { key: 'name', label: 'Role name', type: 'text' }
  ];

  protected readonly form = this.formBuilder.group({
    name: ['', Validators.required]
  });

  protected readonly detailItems = computed<AdminDetailItem[]>(() => {
    const role = this.selectedRole();
    if (!role) {
      return [];
    }

    return [
      { label: 'Role ID', value: role.id },
      { label: 'Name', value: role.name },
      { label: 'Normalized name', value: role.normalizedName },
      { label: 'Assigned users', value: role.userCount }
    ];
  });

  constructor() {
    this.loadRoles();
    this.actionBus.actions$.subscribe((action) => {
      if (action === 'create-role' && this.canCreate()) {
        this.openCreate();
      }
    });
  }

  protected loadRoles(): void {
    this.loading.set(true);

    this.api
      .getRoles()
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe((roles) => this.roles.set(roles));
  }

  protected openCreate(): void {
    this.editingRoleId.set(null);
    this.form.reset({ name: '' });
    this.formVisible.set(true);
  }

  protected closeForm(visible: boolean): void {
    this.formVisible.set(visible);
    if (!visible) {
      this.editingRoleId.set(null);
    }
  }

  protected handleAction(actionId: string, row: RoleDto): void {
    switch (actionId) {
      case 'view':
        this.api.getRole(row.id).subscribe((role) => {
          this.selectedRole.set(role);
          this.detailsVisible.set(true);
        });
        break;
      case 'edit':
        this.editingRoleId.set(row.id);
        this.form.reset({ name: row.name });
        this.formVisible.set(true);
        break;
      case 'delete':
        this.pendingDeleteRole.set(row);
        this.deleteDialogVisible.set(true);
        break;
    }
  }

  protected closeDeleteDialog(visible: boolean): void {
    this.deleteDialogVisible.set(visible);
    if (!visible && !this.deleting()) {
      this.pendingDeleteRole.set(null);
    }
  }

  protected confirmDelete(): void {
    const role = this.pendingDeleteRole();

    if (!role || this.deleting()) {
      return;
    }

    this.deleting.set(true);
    this.api
      .deleteRole(role.id)
      .pipe(finalize(() => this.deleting.set(false)))
      .subscribe(() => {
        this.toast.danger('Role deleted', role.name);
        this.deleteDialogVisible.set(false);
        this.pendingDeleteRole.set(null);
        this.loadRoles();
      });
  }

  protected save(): void {
    if (this.form.invalid || this.saving()) {
      return;
    }

    this.saving.set(true);
    const request = {
      name: this.form.getRawValue().name
    };

    const saveRequest = this.editingRoleId()
      ? this.api.updateRole(this.editingRoleId()!, request as UpdateRoleRequest)
      : this.api.createRole(request as CreateRoleCommand);

    saveRequest
      .pipe(finalize(() => this.saving.set(false)))
      .subscribe(() => {
        this.toast.success(this.editingRoleId() ? 'Role updated' : 'Role created', request.name);
        this.formVisible.set(false);
        this.loadRoles();
      });
  }

  protected saveMatrixPlaceholder(): void {
    this.toast.success('Permission matrix saved', 'Matrix is local template state until a permissions endpoint is added.');
  }

  protected exportRows(fileName: string, rows: RoleDto[]): void {
    exportCsv(fileName, rows as unknown as ExportRow[]);
  }

  protected exportRowsJson(fileName: string, rows: RoleDto[]): void {
    exportJson(fileName, rows as unknown as ExportRow[]);
  }
}

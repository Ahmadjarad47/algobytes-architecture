import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize } from 'rxjs';

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
import { RolesApiService } from '../../api/roles-api.service';
import { CreateRoleCommand, RoleDetailsDto, RoleDto, UpdateRoleRequest } from '../../models/roles.models';

@Component({
  selector: 'app-roles-list',
  imports: [
    ReactiveFormsModule,
    AdminDataTable,
    AdminFormDialog,
    AdminDetailsDrawer,
    AdminConfirmDialog
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
      searchPlaceholder="Search roles"
      emptyTitle="No roles configured"
      emptyMessage="Create a role to start organizing access."
      [actions]="actions"
      (refresh)="loadRoles()"
      (create)="openCreate()"
      (rowAction)="handleAction($event.actionId, $event.row)"
    />

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

  protected readonly columns: AdminTableColumn[] = [
    { field: 'name', header: 'Name', sortable: true, filter: true },
    { field: 'normalizedName', header: 'Normalized name', sortable: true }
  ];

  protected readonly actions: AdminRowAction<RoleDto>[] = [
    { id: 'view', label: 'View role', icon: 'pi pi-eye' },
    { id: 'edit', label: 'Edit role', icon: 'pi pi-pencil' },
    { id: 'delete', label: 'Delete role', icon: 'pi pi-trash', severity: 'danger' }
  ];

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
}

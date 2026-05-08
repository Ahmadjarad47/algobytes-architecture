import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import { TableLazyLoadEvent } from 'primeng/table';

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
import { toTableQuery } from '../../../../shared/utils/admin-table.utils';
import { AppToastService } from '../../../../core/services/app-toast.service';
import { UsersApiService } from '../../api/users-api.service';
import {
  CreateUserCommand,
  UpdateUserRequest,
  UserDetails,
  UserListItem
} from '../../models/users.models';
import { RolesApiService } from '../../../roles/api/roles-api.service';
import { RoleDto } from '../../../roles/models/roles.models';

@Component({
  selector: 'app-users-list',
  imports: [
    ReactiveFormsModule,
    AdminDataTable,
    AdminFormDialog,
    AdminDetailsDrawer,
    AdminConfirmDialog
  ],
  template: `
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
      searchPlaceholder="Search users"
      emptyTitle="No users found"
      emptyMessage="Try a different filter combination or add a new user."
      [actions]="actions"
      (lazyLoad)="loadUsers($event)"
      (refresh)="reload()"
      (create)="openCreate()"
      (rowAction)="handleAction($event.actionId, $event.row)"
    />

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
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class UsersList {
  private readonly api = inject(UsersApiService);
  private readonly rolesApi = inject(RolesApiService);
  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly toast = inject(AppToastService);

  protected readonly users = signal<UserListItem[]>([]);
  protected readonly loading = signal(false);
  protected readonly saving = signal(false);
  protected readonly pageSize = signal(25);
  protected readonly first = signal(0);
  protected readonly totalRecords = signal(0);
  protected readonly formVisible = signal(false);
  protected readonly detailsVisible = signal(false);
  protected readonly editingUserId = signal<string | null>(null);
  protected readonly selectedUser = signal<UserDetails | null>(null);
  protected readonly deleteDialogVisible = signal(false);
  protected readonly deleting = signal(false);
  protected readonly pendingDeleteUser = signal<UserListItem | null>(null);
  protected readonly assignRolesVisible = signal(false);
  protected readonly assigningRoles = signal(false);
  protected readonly pendingAssignUser = signal<UserListItem | null>(null);
  protected readonly roles = signal<RoleDto[]>([]);

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
      field: 'roles',
      header: 'Roles',
      cellType: 'list'
    }
  ];

  protected readonly actions: AdminRowAction<UserListItem>[] = [
    { id: 'view', label: 'View details', icon: 'pi pi-eye' },
    { id: 'edit', label: 'Edit user', icon: 'pi pi-pencil' },
    { id: 'assign-roles', label: 'Assign roles', icon: 'pi pi-user-plus' },
    { id: 'toggle-active', label: 'Toggle active', icon: 'pi pi-power-off', severity: 'warn' },
    { id: 'delete', label: 'Delete user', icon: 'pi pi-trash', severity: 'danger' }
  ];

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
      { label: 'Created', value: user.createdAt, type: 'date' },
      { label: 'Last login', value: user.lastLoginAt, type: 'date' }
    ];
  });

  private lastLazyEvent: TableLazyLoadEvent = {
    first: 0,
    rows: 25
  };

  constructor() {
    this.loadRoles();
    this.loadUsers(this.lastLazyEvent);
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
          this.users.set(response.items);
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
      case 'delete':
        this.pendingDeleteUser.set(row);
        this.deleteDialogVisible.set(true);
        break;
    }
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

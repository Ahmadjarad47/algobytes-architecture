import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormControl, FormsModule, NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
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
import { CustomFieldDefinitionsApiService } from '../../../custom-fields/api/custom-field-definitions-api.service';
import { CustomFieldDefinition } from '../../../custom-fields/models/custom-fields.models';
import {
  customFieldColumns,
  customFieldControlKey,
  customFieldDetailItems,
  customFieldFormFields,
  customFieldInitialValues,
  customFieldsPayload
} from '../../../custom-fields/utils/custom-field.utils';
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
    <section class="surface-card dashboard-section mb-3">
      <div class="flex flex-wrap items-center justify-between gap-3">
        <div>
          <div class="text-[11px] font-semibold uppercase tracking-wide text-slate-500">Role lifecycle</div>
          <div class="mt-1 text-sm font-semibold text-slate-950">Active roles and trash retention</div>
        </div>

        <div class="flex flex-wrap items-center gap-2">
          <button
            type="button"
            class="rounded-full px-3 py-1.5 text-xs font-semibold transition"
            [class]="!showTrashed() ? 'bg-slate-900 text-white' : 'bg-slate-100 text-slate-600 hover:bg-slate-200'"
            (click)="setTrashView(false)"
          >
            Active roles
          </button>
          <button
            type="button"
            class="rounded-full px-3 py-1.5 text-xs font-semibold transition"
            [class]="showTrashed() ? 'bg-rose-600 text-white' : 'bg-rose-50 text-rose-700 hover:bg-rose-100'"
            (click)="setTrashView(true)"
          >
            Trash
          </button>
        </div>
      </div>
    </section>

    <app-admin-data-table
      title="Roles"
      [subtitle]="tableSubtitle()"
      [columns]="columns()"
      [value]="roles()"
      [loading]="loading()"
      [lazy]="false"
      [rows]="25"
      [totalRecords]="roles().length"
      [globalFilterFields]="globalFilterFields()"
      [showCreate]="canCreate() && !showTrashed()"
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
      [fields]="fields()"
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
      title="Move role to trash"
      [message]="'Move ' + (pendingDeleteRole()?.name ?? 'this role') + ' to trash?'"
      description="The role will stay in trash for 3 days before final soft delete."
      confirmLabel="Move to trash"
      [loading]="deleting()"
      (visibleChange)="closeDeleteDialog($event)"
      (confirm)="confirmDelete()"
    />
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class RolesList {
  private readonly api = inject(RolesApiService);
  private readonly customFieldDefinitionsApi = inject(CustomFieldDefinitionsApiService);
  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly toast = inject(AppToastService);
  private readonly actionBus = inject(AdminActionBusService);
  private readonly permissionService = inject(PermissionService);

  protected readonly roles = signal<RoleDto[]>([]);
  protected readonly loading = signal(false);
  protected readonly saving = signal(false);
  protected readonly formVisible = signal(false);
  protected readonly detailsVisible = signal(false);
  protected readonly showTrashed = signal(false);
  protected readonly editingRoleId = signal<string | null>(null);
  protected readonly selectedRole = signal<RoleDetailsDto | null>(null);
  protected readonly deleteDialogVisible = signal(false);
  protected readonly deleting = signal(false);
  protected readonly pendingDeleteRole = signal<RoleDto | null>(null);
  protected readonly customFieldDefinitions = signal<CustomFieldDefinition[]>([]);
  protected readonly permissionActions: readonly PermissionAction[] = ['View', 'Create', 'Edit', 'Delete'];
  protected readonly permissionMatrix: PermissionMatrixRow[] = [
    { module: 'Users', permissions: { View: true, Create: true, Edit: true, Delete: true } },
    { module: 'Roles', permissions: { View: true, Create: true, Edit: true, Delete: null } },
    { module: 'Access Policies', permissions: { View: true, Create: true, Edit: true, Delete: true } },
    { module: 'Logs', permissions: { View: true, Create: null, Edit: null, Delete: null } },
    { module: 'Settings', permissions: { View: true, Create: null, Edit: true, Delete: null } }
  ];

  protected readonly globalFilterFields = computed(() => [
    'name',
    'normalizedName',
    ...this.customFieldDefinitions()
      .filter((definition) => definition.searchable)
      .map((definition) => `customFields.${definition.key}`)
  ]);

  protected readonly baseColumns: AdminTableColumn[] = [
    { field: 'name', header: 'Name', sortable: true, filter: true },
    { field: 'normalizedName', header: 'Normalized name', sortable: true },
    { field: 'trashedAt', header: 'Trashed at', cellType: 'date' },
    { field: 'trashExpiresAt', header: 'Trash expires', cellType: 'date' }
  ];

  protected readonly columns = computed<AdminTableColumn[]>(() => [
    ...this.baseColumns,
    ...customFieldColumns(this.customFieldDefinitions())
  ]);

  protected readonly canCreate = computed(() => this.permissionService.can({ any: [Permissions.roles.create] }));
  protected readonly canUpdate = computed(() => this.permissionService.can({ any: [Permissions.roles.update] }));
  protected readonly canDelete = computed(() => this.permissionService.can({ any: [Permissions.roles.delete] }));
  protected readonly canExport = computed(() => this.permissionService.can({ any: [Permissions.roles.read] }));
  protected readonly tableSubtitle = computed(() =>
    this.showTrashed()
      ? 'Trashed roles can be restored for 3 days before final soft delete.'
      : 'Role catalog with reusable table actions and in-place management.'
  );

  protected readonly actions = computed<AdminRowAction<RoleDto>[]>(() => this.showTrashed()
    ? [
        { id: 'view', label: 'View role', icon: 'pi pi-eye' },
        ...(this.canUpdate() ? [{ id: 'restore', label: 'Restore role', icon: 'pi pi-history', severity: 'success' as const } as AdminRowAction<RoleDto>] : [])
      ]
    : [
        { id: 'view', label: 'View role', icon: 'pi pi-eye' },
        ...(this.canUpdate() ? [{ id: 'edit', label: 'Edit role', icon: 'pi pi-pencil' } as AdminRowAction<RoleDto>] : []),
        ...(this.canDelete() ? [{ id: 'delete', label: 'Delete role', icon: 'pi pi-trash', severity: 'danger' as const }] : [])
      ]);

  protected readonly fields = computed<AdminFormField[]>(() => [
    { key: 'name', label: 'Role name', type: 'text' },
    ...customFieldFormFields(this.customFieldDefinitions())
  ]);

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
      { label: 'Assigned users', value: role.userCount },
      { label: 'Trashed at', value: role.trashedAt, type: 'date' },
      { label: 'Trash expires', value: role.trashExpiresAt, type: 'date' },
      ...customFieldDetailItems(this.customFieldDefinitions(), role.customFields)
    ];
  });

  constructor() {
    this.loadCustomFieldDefinitions();
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
      .getRoles({ includeTrashed: this.showTrashed(), onlyTrashed: this.showTrashed() })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe((roles) => this.roles.set(roles));
  }

  protected setTrashView(showTrashed: boolean): void {
    if (this.showTrashed() === showTrashed) {
      return;
    }

    this.showTrashed.set(showTrashed);
    this.loadRoles();
  }

  protected openCreate(): void {
    this.editingRoleId.set(null);
    this.form.reset({ name: '' });
    this.form.patchValue(customFieldInitialValues(this.customFieldDefinitions(), null));
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
        this.form.patchValue(customFieldInitialValues(this.customFieldDefinitions(), row.customFields));
        this.formVisible.set(true);
        break;
      case 'delete':
        this.pendingDeleteRole.set(row);
        this.deleteDialogVisible.set(true);
        break;
      case 'restore':
        this.api.restoreRole(row.id).subscribe(() => {
          this.toast.success('Role restored', row.name);
          this.loadRoles();
        });
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
        this.toast.warn('Moved to trash', `${role.name} will be kept for 3 days.`);
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
    const value = this.form.getRawValue();
    const request = {
      name: value.name,
      customFields: customFieldsPayload(this.customFieldDefinitions(), value)
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

  private loadCustomFieldDefinitions(): void {
    this.customFieldDefinitionsApi
      .getDefinitions('roles')
      .subscribe((definitions) => {
        this.customFieldDefinitions.set(definitions);
        this.syncCustomFieldControls(definitions);
      });
  }

  private syncCustomFieldControls(definitions: readonly CustomFieldDefinition[]): void {
    const dynamicForm = this.form as any;
    const activeKeys = new Set(definitions.map((definition) => customFieldControlKey(definition)));

    for (const definition of definitions) {
      const key = customFieldControlKey(definition);
      const existing = this.form.get(key) as FormControl | null;

      if (existing) {
        existing.setValidators(definition.required ? [Validators.required] : []);
        existing.updateValueAndValidity({ emitEvent: false });
        continue;
      }

      dynamicForm.addControl(
        key,
        new FormControl(
          customFieldInitialValues([definition], null)[key],
          definition.required ? { validators: [Validators.required] } : undefined
        )
      );
    }

    for (const key of Object.keys(this.form.controls).filter((controlKey) => controlKey.startsWith('customField__'))) {
      if (!activeKeys.has(key)) {
        dynamicForm.removeControl(key);
      }
    }
  }
}

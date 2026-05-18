import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormControl, NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import { ButtonModule } from 'primeng/button';
import { DatePickerModule } from 'primeng/datepicker';
import { DrawerModule } from 'primeng/drawer';
import { FloatLabelModule } from 'primeng/floatlabel';
import { FluidModule } from 'primeng/fluid';
import { InputNumberModule } from 'primeng/inputnumber';
import { InputTextModule } from 'primeng/inputtext';
import { MultiSelectModule } from 'primeng/multiselect';
import { SelectModule } from 'primeng/select';
import { TextareaModule } from 'primeng/textarea';
import { ToggleSwitchModule } from 'primeng/toggleswitch';

import { AdminConfirmDialog } from '../../../../shared/components/admin-confirm-dialog/admin-confirm-dialog';
import { AdminDataTable } from '../../../../shared/components/admin-data-table/admin-data-table';
import { AdminDetailsDrawer } from '../../../../shared/components/admin-details-drawer/admin-details-drawer';
import {
  AdminDetailItem,
  AdminFormField,
  AdminFormFieldOption,
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
import { AccessPoliciesApiService } from '../../api/access-policies-api.service';
import { AccessPolicyConditionBuilderComponent } from '../../components/access-policy-condition-builder/access-policy-condition-builder.component';
import {
  AccessPolicyAdminDto,
  AccessPolicyOptionsDto,
  CreateAccessPolicyCommand,
  UpdateAccessPolicyBody
} from '../../models/access-policies.models';

const FALLBACK_POLICY_OPTIONS: AccessPolicyOptionsDto = {
  resources: ['users', 'roles', 'accessPolicies', 'logs', 'errorLogs', '*'],
  actionsByResource: {
    users: ['read', 'create', 'update', 'delete'],
    roles: ['read', 'create', 'update', 'delete'],
    accessPolicies: ['read', 'create', 'update', 'delete'],
    logs: ['read', 'create', 'update', 'delete'],
    errorLogs: ['read', 'create', 'update', 'delete'],
    '*': ['*']
  },
  effects: [
    { value: 0, label: 'Allow' },
    { value: 1, label: 'Deny' }
  ],
  subjectTypes: [
    { value: 0, label: 'User' },
    { value: 1, label: 'Role' },
    { value: 2, label: 'Authenticated' },
    { value: 3, label: 'Everyone' }
  ],
  conditionFieldsByResource: {}
};

@Component({
  selector: 'app-access-policies-list',
  imports: [
    ReactiveFormsModule,
    AdminDataTable,
    AdminDetailsDrawer,
    AdminConfirmDialog,
    AccessPolicyConditionBuilderComponent,
    ButtonModule,
    DatePickerModule,
    DrawerModule,
    FloatLabelModule,
    FluidModule,
    InputNumberModule,
    InputTextModule,
    MultiSelectModule,
    SelectModule,
    TextareaModule,
    ToggleSwitchModule
  ],
  template: `
    <section class="surface-card dashboard-section mb-3">
      <div class="flex flex-wrap items-center justify-between gap-3">
        <div>
          <div class="text-[11px] font-semibold uppercase tracking-wide text-slate-500">Policy lifecycle</div>
          <div class="mt-1 text-sm font-semibold text-slate-950">Active policies and trash retention</div>
        </div>

        <div class="flex flex-wrap items-center gap-2">
          <button
            type="button"
            class="rounded-full px-3 py-1.5 text-xs font-semibold transition"
            [class]="!showTrashed() ? 'bg-slate-900 text-white' : 'bg-slate-100 text-slate-600 hover:bg-slate-200'"
            (click)="setTrashView(false)"
          >
            Active policies
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
      title="Access Policies"
      [subtitle]="tableSubtitle()"
      [columns]="columns()"
      [value]="policies()"
      [loading]="loading()"
      [lazy]="false"
      [rows]="25"
      [totalRecords]="policies().length"
      [globalFilterFields]="globalFilterFields()"
      [showCreate]="canCreate() && !showTrashed()"
      [showExport]="canExport()"
      searchPlaceholder="Search policies"
      emptyTitle="No access policies found"
      emptyMessage="Create your first policy to define authorization rules."
      [actions]="actions()"
      (refresh)="loadPolicies()"
      (create)="openCreate()"
      (rowAction)="handleAction($event.actionId, $event.row)"
      (exportCsv)="exportRows('access-policies', $event)"
      (exportJson)="exportRowsJson('access-policies', $event)"
    />

    <p-drawer
      position="right"
      [visible]="formVisible()"
      [header]="editingPolicyId() ? 'Edit policy' : 'Create policy'"
      [modal]="true"
      [dismissible]="true"
      [blockScroll]="true"
      [appendTo]="'body'"
      [style]="{ width: 'min(52rem, 100vw)' }"
      styleClass="surface-dialog app-details-drawer"
      (visibleChange)="closeForm($event)"
    >
      <form [formGroup]="form" class="flex flex-col gap-4" (ngSubmit)="save()">
        <p-fluid>
          <div class="grid gap-3 md:grid-cols-2">
            <p-floatlabel variant="on">
              <p-select
                formControlName="resource"
                [options]="resourceOptions()"
                optionLabel="label"
                optionValue="value"
                appendTo="body"
                class="w-full"
              />
              <label>Resource</label>
            </p-floatlabel>

            <p-floatlabel variant="on">
              <p-select
                formControlName="action"
                [options]="actionOptions()"
                optionLabel="label"
                optionValue="value"
                appendTo="body"
                class="w-full"
              />
              <label>Action</label>
            </p-floatlabel>

            <p-floatlabel variant="on">
              <input pInputText formControlName="subjectKey" class="w-full" />
              <label>Subject key</label>
            </p-floatlabel>

            <p-floatlabel variant="on">
              <p-select
                formControlName="subjectType"
                [options]="subjectTypeOptions()"
                optionLabel="label"
                optionValue="value"
                appendTo="body"
                class="w-full"
              />
              <label>Subject type</label>
            </p-floatlabel>

            <p-floatlabel variant="on">
              <p-select
                formControlName="effect"
                [options]="effectOptions()"
                optionLabel="label"
                optionValue="value"
                appendTo="body"
                class="w-full"
              />
              <label>Effect</label>
            </p-floatlabel>

            <p-floatlabel variant="on">
              <p-inputnumber formControlName="priority" inputStyleClass="w-full" />
              <label>Priority</label>
            </p-floatlabel>

            <p-floatlabel variant="on" class="md:col-span-2">
              <textarea pTextarea formControlName="description" rows="2" class="w-full resize-y"></textarea>
              <label>Description</label>
            </p-floatlabel>

            <p-floatlabel variant="on">
              <p-datepicker
                formControlName="validFrom"
                [showIcon]="true"
                appendTo="body"
                styleClass="w-full"
                inputStyleClass="w-full"
              />
              <label>Valid from</label>
            </p-floatlabel>

            <p-floatlabel variant="on">
              <p-datepicker
                formControlName="validTo"
                [showIcon]="true"
                appendTo="body"
                styleClass="w-full"
                inputStyleClass="w-full"
              />
              <label>Valid to</label>
            </p-floatlabel>

            <label class="flex items-center justify-between rounded-lg border border-surface-200 px-3 py-2.5 md:col-span-2">
              <span class="text-sm font-medium text-surface-700">Enabled</span>
              <p-toggleswitch formControlName="isEnabled" />
            </label>

            <app-access-policy-condition-builder
              class="md:col-span-2"
              [resource]="form.controls.resource.value"
              [fields]="conditionFields()"
              [conditionJson]="conditionJson()"
              [validationMessage]="conditionValidationMessage()"
              [validationSeverity]="conditionValidationSeverity()"
              (conditionJsonChange)="setConditionJson($event)"
              (completeChange)="conditionComplete.set($event)"
              (advancedChange)="advancedCondition.set($event)"
              (validateRequested)="validateCondition()"
            />

            @for (field of customFormFields(); track field.key) {
              <div [class]="field.type === 'json' ? 'md:col-span-2' : ''">
                @if (field.type === 'switch') {
                  <label class="flex items-center justify-between rounded-lg border border-surface-200 px-3 py-2.5">
                    <span class="text-sm font-medium text-surface-700">{{ field.label }}</span>
                    <p-toggleswitch [formControl]="customFieldControl(field.key)" />
                  </label>
                } @else {
                  <p-floatlabel variant="on">
                    @switch (field.type) {
                      @case ('number') {
                        <p-inputnumber [formControl]="customFieldControl(field.key)" inputStyleClass="w-full" />
                      }
                      @case ('date') {
                        <p-datepicker
                          [formControl]="customFieldControl(field.key)"
                          [showIcon]="true"
                          appendTo="body"
                          styleClass="w-full"
                          inputStyleClass="w-full"
                        />
                      }
                      @case ('select') {
                        <p-select
                          [formControl]="customFieldControl(field.key)"
                          [options]="field.options ?? []"
                          optionLabel="label"
                          optionValue="value"
                          appendTo="body"
                          class="w-full"
                        />
                      }
                      @case ('multiselect') {
                        <p-multiselect
                          [formControl]="customFieldControl(field.key)"
                          [options]="field.options ?? []"
                          optionLabel="label"
                          optionValue="value"
                          appendTo="body"
                          class="w-full"
                        />
                      }
                      @case ('json') {
                        <textarea
                          pTextarea
                          [formControl]="customFieldControl(field.key)"
                          rows="5"
                          class="w-full resize-y font-mono text-xs"
                        ></textarea>
                      }
                      @default {
                        <input pInputText [formControl]="customFieldControl(field.key)" class="w-full" />
                      }
                    }
                    <label>{{ field.label }}{{ field.required ? ' *' : '' }}</label>
                  </p-floatlabel>
                }
              </div>
            }
          </div>
        </p-fluid>

        <div class="flex justify-end gap-2 border-t border-surface-200 pt-3">
          <p-button
            label="Cancel"
            severity="secondary"
            size="small"
            [outlined]="true"
            type="button"
            (onClick)="closeForm(false)"
          />
          <p-button
            [label]="editingPolicyId() ? 'Save changes' : 'Create policy'"
            size="small"
            type="submit"
            [disabled]="form.invalid || saving() || !conditionComplete() || conditionValidationSeverity() === 'error'"
            [loading]="saving()"
          />
        </div>
      </form>
    </p-drawer>

    <app-admin-details-drawer
      [visible]="detailsVisible()"
      [title]="selectedPolicy()?.resource ?? 'Policy details'"
      [items]="detailItems()"
      (visibleChange)="detailsVisible.set($event)"
    />

    <app-admin-confirm-dialog
      [visible]="deleteDialogVisible()"
      title="Move policy to trash"
      [message]="'Move ' + (pendingDeletePolicy()?.resource ?? 'this policy') + ' policy to trash?'"
      description="The policy will stay in trash for 3 days before final soft delete."
      confirmLabel="Move to trash"
      [loading]="deleting()"
      (visibleChange)="closeDeleteDialog($event)"
      (confirm)="confirmDelete()"
    />
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AccessPoliciesList {
  private readonly api = inject(AccessPoliciesApiService);
  private readonly customFieldDefinitionsApi = inject(CustomFieldDefinitionsApiService);
  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly toast = inject(AppToastService);
  private readonly actionBus = inject(AdminActionBusService);
  private readonly permissionService = inject(PermissionService);

  protected readonly policies = signal<AccessPolicyAdminDto[]>([]);
  protected readonly loading = signal(false);
  protected readonly saving = signal(false);
  protected readonly formVisible = signal(false);
  protected readonly detailsVisible = signal(false);
  protected readonly showTrashed = signal(false);
  protected readonly editingPolicyId = signal<string | null>(null);
  protected readonly selectedPolicy = signal<AccessPolicyAdminDto | null>(null);
  protected readonly deleteDialogVisible = signal(false);
  protected readonly deleting = signal(false);
  protected readonly pendingDeletePolicy = signal<AccessPolicyAdminDto | null>(null);
  protected readonly options = signal<AccessPolicyOptionsDto>(FALLBACK_POLICY_OPTIONS);
  protected readonly conditionJson = signal<string | null>(null);
  protected readonly conditionComplete = signal(true);
  protected readonly advancedCondition = signal(false);
  protected readonly conditionValidationMessage = signal<string | null>(null);
  protected readonly conditionValidationSeverity = signal<'success' | 'error' | 'info' | 'warn'>('info');
  protected readonly selectedResource = signal('');
  protected readonly customFieldDefinitions = signal<CustomFieldDefinition[]>([]);

  protected readonly globalFilterFields = computed(() => [
    'resource',
    'action',
    'subjectKey',
    'description',
    ...this.customFieldDefinitions()
      .filter((definition) => definition.searchable)
      .map((definition) => `customFields.${definition.key}`)
  ]);

  protected readonly baseColumns: AdminTableColumn[] = [
    { field: 'resource', header: 'Resource', sortable: true, filter: true },
    { field: 'action', header: 'Action', sortable: true, filter: true },
    { field: 'subjectKey', header: 'Subject key', filter: true },
    { field: 'effect', header: 'Effect' },
    { field: 'subjectType', header: 'Subject type' },
    { field: 'priority', header: 'Priority', sortable: true, filter: true, filterType: 'numeric' },
    {
      field: 'isEnabled',
      header: 'Enabled',
      filter: true,
      filterType: 'boolean',
      cellType: 'boolean'
    },
    { field: 'validTo', header: 'Valid to', cellType: 'date' },
    { field: 'trashedAt', header: 'Trashed at', cellType: 'date' },
    { field: 'trashExpiresAt', header: 'Trash expires', cellType: 'date' }
  ];

  protected readonly columns = computed<AdminTableColumn[]>(() => [
    ...this.baseColumns,
    ...customFieldColumns(this.customFieldDefinitions())
  ]);

  protected readonly canCreate = computed(() => this.permissionService.can({ any: [Permissions.accessPolicies.create] }));
  protected readonly canUpdate = computed(() => this.permissionService.can({ any: [Permissions.accessPolicies.update] }));
  protected readonly canDelete = computed(() => this.permissionService.can({ any: [Permissions.accessPolicies.delete] }));
  protected readonly canExport = computed(() => this.permissionService.can({ any: [Permissions.accessPolicies.read] }));
  protected readonly tableSubtitle = computed(() =>
    this.showTrashed()
      ? 'Policies currently in trash. They can be restored for 3 days before final soft delete.'
      : 'Policy rules backed by the admin API with reusable list management primitives.'
  );

  protected readonly actions = computed<AdminRowAction<AccessPolicyAdminDto>[]>(() => this.showTrashed()
    ? [
        { id: 'view', label: 'View policy', icon: 'pi pi-eye' },
        ...(this.canUpdate() ? [{ id: 'restore', label: 'Restore policy', icon: 'pi pi-history', severity: 'success' as const } as AdminRowAction<AccessPolicyAdminDto>] : [])
      ]
    : [
        { id: 'view', label: 'View policy', icon: 'pi pi-eye' },
        ...(this.canUpdate() ? [
          { id: 'edit', label: 'Edit policy', icon: 'pi pi-pencil' } as AdminRowAction<AccessPolicyAdminDto>,
          { id: 'toggle', label: 'Toggle policy', icon: 'pi pi-shield', severity: 'warn' as const }
        ] : []),
        ...(this.canDelete() ? [{ id: 'delete', label: 'Delete policy', icon: 'pi pi-trash', severity: 'danger' as const }] : [])
      ]);

  protected readonly resourceOptions = computed(() =>
    this.options().resources.map((resource) => ({
      label: this.toDisplayLabel(resource),
      value: resource
    }))
  );

  protected readonly actionOptions = computed(() => {
    const resource = this.selectedResource();
    const actions = this.options().actionsByResource?.[resource] ?? [];

    return actions.map((action) => ({
      label: this.toDisplayLabel(action),
      value: action
    }));
  });

  protected readonly conditionFields = computed(
    () => this.options().conditionFieldsByResource?.[this.selectedResource()] ?? []
  );

  protected readonly effectOptions = computed(() => this.toFormOptions(this.options().effects));

  protected readonly subjectTypeOptions = computed(() => this.toFormOptions(this.options().subjectTypes));
  protected readonly customFormFields = computed<AdminFormField[]>(() => customFieldFormFields(this.customFieldDefinitions()));

  protected readonly form = this.formBuilder.group({
    resource: ['', Validators.required],
    action: ['', Validators.required],
    subjectKey: ['', Validators.required],
    effect: [FALLBACK_POLICY_OPTIONS.effects[0]?.value ?? 0, Validators.required],
    subjectType: [FALLBACK_POLICY_OPTIONS.subjectTypes[0]?.value ?? 0, Validators.required],
    priority: [0],
    description: [''],
    conditionJson: [''],
    validFrom: [null as Date | null],
    validTo: [null as Date | null],
    isEnabled: [true]
  });

  protected readonly detailItems = computed<AdminDetailItem[]>(() => {
    const policy = this.selectedPolicy();
    if (!policy) {
      return [];
    }

    return [
      { label: 'Policy ID', value: policy.id },
      { label: 'Resource', value: policy.resource },
      { label: 'Action', value: policy.action },
      { label: 'Subject key', value: policy.subjectKey },
      { label: 'Effect', value: policy.effect },
      { label: 'Subject type', value: policy.subjectType },
      { label: 'Priority', value: policy.priority },
      {
        label: 'Enabled',
        value: policy.isEnabled ? 'Enabled' : 'Disabled',
        type: 'status',
        severity: policy.isEnabled ? 'success' : 'secondary'
      },
      { label: 'Description', value: policy.description },
      { label: 'Condition JSON', value: policy.conditionJson, type: 'json' },
      { label: 'Valid from', value: policy.validFrom, type: 'date' },
      { label: 'Valid to', value: policy.validTo, type: 'date' },
      { label: 'Trashed at', value: policy.trashedAt, type: 'date' },
      { label: 'Trash expires', value: policy.trashExpiresAt, type: 'date' },
      ...customFieldDetailItems(this.customFieldDefinitions(), policy.customFields)
    ];
  });

  constructor() {
    this.form.controls.resource.valueChanges.subscribe((resource) => {
      this.selectedResource.set(resource);
      this.clearConditionValidation();

      const availableActions = this.options().actionsByResource?.[resource] ?? [];
      if (availableActions.length > 0 && !availableActions.includes(this.form.controls.action.value)) {
        this.form.controls.action.setValue(availableActions[0]);
      }

      if (this.conditionJson() || !this.options().conditionFieldsByResource?.[resource]?.length) {
        this.setConditionJson(null);
      }
    });
    this.loadCustomFieldDefinitions();
    this.loadOptions();
    this.loadPolicies();
    this.actionBus.actions$.subscribe((action) => {
      if (action === 'create-access-policy' && this.canCreate()) {
        this.openCreate();
      }
    });
  }

  protected loadOptions(): void {
    this.api.getOptions().subscribe({
      next: (options) => {
        this.options.set({
          ...options,
          effects: options.effects ?? options.effectOptions ?? [],
          subjectTypes: options.subjectTypes ?? options.subjectTypeOptions ?? [],
          actionsByResource: options.actionsByResource ?? FALLBACK_POLICY_OPTIONS.actionsByResource,
          conditionFieldsByResource:
            options.conditionFieldsByResource ?? FALLBACK_POLICY_OPTIONS.conditionFieldsByResource
        });
        this.selectedResource.set(this.form.controls.resource.value);
      },
      error: () => this.options.set(FALLBACK_POLICY_OPTIONS)
    });
  }

  protected loadPolicies(): void {
    this.loading.set(true);
    this.api
      .getPolicies({ includeTrashed: this.showTrashed(), onlyTrashed: this.showTrashed() })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe((policies) => this.policies.set(policies));
  }

  protected setTrashView(showTrashed: boolean): void {
    if (this.showTrashed() === showTrashed) {
      return;
    }

    this.showTrashed.set(showTrashed);
    this.loadPolicies();
  }

  protected openCreate(): void {
    this.editingPolicyId.set(null);
    const resource = this.options().resources[0] ?? '';
    const action = this.options().actionsByResource?.[resource]?.[0] ?? '';
    this.form.reset({
      resource,
      action,
      subjectKey: '',
      effect: this.options().effects[0]?.value ?? FALLBACK_POLICY_OPTIONS.effects[0].value,
      subjectType:
        this.options().subjectTypes[0]?.value ??
        FALLBACK_POLICY_OPTIONS.subjectTypes[0].value,
      priority: 0,
      description: '',
      conditionJson: '',
      validFrom: null,
      validTo: null,
      isEnabled: true
    });
    this.form.patchValue(customFieldInitialValues(this.customFieldDefinitions(), null));
    this.selectedResource.set(resource);
    this.setConditionJson(null);
    this.clearConditionValidation();
    this.formVisible.set(true);
  }

  protected closeForm(visible: boolean): void {
    this.formVisible.set(visible);
    if (!visible) {
      this.editingPolicyId.set(null);
      this.clearConditionValidation();
    }
  }

  protected handleAction(actionId: string, row: AccessPolicyAdminDto): void {
    switch (actionId) {
      case 'view':
        this.api.getPolicy(row.id).subscribe((policy) => {
          this.selectedPolicy.set(policy);
          this.detailsVisible.set(true);
        });
        break;
      case 'edit':
        this.editingPolicyId.set(row.id);
        this.form.reset({
          resource: row.resource,
          action: row.action,
          subjectKey: row.subjectKey,
          effect: row.effect,
          subjectType: row.subjectType,
          priority: row.priority ?? 0,
          description: row.description ?? '',
          conditionJson: row.conditionJson ?? '',
          validFrom: row.validFrom ? new Date(row.validFrom) : null,
          validTo: row.validTo ? new Date(row.validTo) : null,
          isEnabled: row.isEnabled
        });
        this.form.patchValue(customFieldInitialValues(this.customFieldDefinitions(), row.customFields));
        this.selectedResource.set(row.resource);
        this.setConditionJson(row.conditionJson ?? null);
        this.clearConditionValidation();
        this.formVisible.set(true);
        break;
      case 'toggle':
        this.api.setEnabled(row.id, !row.isEnabled).subscribe(() => {
          const toast = row.isEnabled ? this.toast.warn.bind(this.toast) : this.toast.success.bind(this.toast);
          toast(row.isEnabled ? 'Policy disabled' : 'Policy enabled', row.resource);
          this.loadPolicies();
        });
        break;
      case 'delete':
        this.pendingDeletePolicy.set(row);
        this.deleteDialogVisible.set(true);
        break;
      case 'restore':
        this.api.restorePolicy(row.id).subscribe(() => {
          this.toast.success('Policy restored', row.resource);
          this.loadPolicies();
        });
        break;
    }
  }

  protected closeDeleteDialog(visible: boolean): void {
    this.deleteDialogVisible.set(visible);
    if (!visible && !this.deleting()) {
      this.pendingDeletePolicy.set(null);
    }
  }

  protected confirmDelete(): void {
    const policy = this.pendingDeletePolicy();

    if (!policy || this.deleting()) {
      return;
    }

    this.deleting.set(true);
    this.api
      .deletePolicy(policy.id)
      .pipe(finalize(() => this.deleting.set(false)))
      .subscribe(() => {
        this.toast.warn('Moved to trash', `${policy.resource} policy will be kept for 3 days.`);
        this.deleteDialogVisible.set(false);
        this.pendingDeletePolicy.set(null);
        this.loadPolicies();
      });
  }

  protected save(): void {
    if (this.form.invalid || this.saving() || !this.conditionComplete()) {
      return;
    }

    if (this.conditionJson() && this.conditionValidationSeverity() !== 'success') {
      this.validateCondition(() => this.saveValidated());
      return;
    }

    this.saveValidated();
  }

  protected setConditionJson(value: string | null): void {
    this.conditionJson.set(value);
    this.form.controls.conditionJson.setValue(value ?? '');
    this.clearConditionValidation();
  }

  protected validateCondition(onValid?: () => void): void {
    const conditionJson = this.conditionJson();
    const resource = this.form.controls.resource.value;

    if (!conditionJson) {
      this.conditionValidationSeverity.set('success');
      this.conditionValidationMessage.set('No condition JSON will be sent.');
      this.toast.info('Condition validation', 'No condition JSON will be sent.');
      onValid?.();
      return;
    }

    this.conditionValidationSeverity.set('info');
    this.conditionValidationMessage.set('Validating condition...');

    this.api.validateCondition({ resource, conditionJson }).subscribe((result) => {
      this.conditionValidationSeverity.set(result.isValid ? 'success' : 'error');
      this.conditionValidationMessage.set(result.isValid ? 'Condition is valid.' : result.errorMessage);
      this.toast[result.isValid ? 'success' : 'error'](
        result.isValid ? 'Condition is valid' : 'Condition is invalid',
        result.isValid ? undefined : result.errorMessage ?? undefined
      );

      if (result.isValid) {
        onValid?.();
      }
    });
  }

  private saveValidated(): void {
    this.saving.set(true);
    const request = this.toRequestBody();
    const saveRequest = this.editingPolicyId()
      ? this.api.updatePolicy(this.editingPolicyId()!, request as UpdateAccessPolicyBody)
      : this.api.createPolicy(request as CreateAccessPolicyCommand);

    saveRequest
      .pipe(finalize(() => this.saving.set(false)))
      .subscribe(() => {
        this.toast.success(
          this.editingPolicyId() ? 'Policy updated' : 'Policy created',
          request.resource
        );
        this.formVisible.set(false);
        this.loadPolicies();
      });
  }

  private clearConditionValidation(): void {
    this.conditionValidationMessage.set(null);
    this.conditionValidationSeverity.set('info');
  }

  private toRequestBody(): CreateAccessPolicyCommand {
    const value = this.form.getRawValue();

    return {
      resource: value.resource,
      action: value.action,
      subjectKey: value.subjectKey,
      effect: value.effect,
      subjectType: value.subjectType,
      priority: value.priority,
      description: value.description || null,
      conditionJson: this.conditionJson(),
      validFrom: value.validFrom?.toISOString() ?? null,
      validTo: value.validTo?.toISOString() ?? null,
      isEnabled: value.isEnabled,
      customFields: customFieldsPayload(this.customFieldDefinitions(), value)
    };
  }

  private toFormOptions(
    options: readonly { readonly value: string | number; readonly label: string }[] = []
  ): AdminFormFieldOption[] {
    return options.map((option) => ({
      label: this.toDisplayLabel(option.label),
      value: option.value
    }));
  }

  private toDisplayLabel(value: string): string {
    if (value === '*') {
      return 'Wildcard';
    }

    return value
      .replace(/([a-z])([A-Z])/g, '$1 $2')
      .replace(/[_-]+/g, ' ')
      .replace(/^\w/, (letter) => letter.toUpperCase());
  }

  protected exportRows(fileName: string, rows: AccessPolicyAdminDto[]): void {
    exportCsv(fileName, rows as unknown as ExportRow[]);
  }

  protected exportRowsJson(fileName: string, rows: AccessPolicyAdminDto[]): void {
    exportJson(fileName, rows as unknown as ExportRow[]);
  }

  protected customFieldControl(key: string): FormControl {
    return this.form.get(key) as FormControl;
  }

  private loadCustomFieldDefinitions(): void {
    this.customFieldDefinitionsApi
      .getDefinitions('accessPolicies')
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

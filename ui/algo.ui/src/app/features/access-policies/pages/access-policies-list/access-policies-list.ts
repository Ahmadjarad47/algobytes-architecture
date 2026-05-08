import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import { ButtonModule } from 'primeng/button';
import { DatePickerModule } from 'primeng/datepicker';
import { DialogModule } from 'primeng/dialog';
import { FloatLabelModule } from 'primeng/floatlabel';
import { FluidModule } from 'primeng/fluid';
import { InputNumberModule } from 'primeng/inputnumber';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { TextareaModule } from 'primeng/textarea';
import { ToggleSwitchModule } from 'primeng/toggleswitch';

import { AdminConfirmDialog } from '../../../../shared/components/admin-confirm-dialog/admin-confirm-dialog';
import { AdminDataTable } from '../../../../shared/components/admin-data-table/admin-data-table';
import { AdminDetailsDrawer } from '../../../../shared/components/admin-details-drawer/admin-details-drawer';
import {
  AdminDetailItem,
  AdminFormFieldOption,
  AdminRowAction,
  AdminTableColumn
} from '../../../../shared/models/admin-table.model';
import { AppToastService } from '../../../../core/services/app-toast.service';
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
    DialogModule,
    FloatLabelModule,
    FluidModule,
    InputNumberModule,
    InputTextModule,
    SelectModule,
    TextareaModule,
    ToggleSwitchModule
  ],
  template: `
    <app-admin-data-table
      title="Access Policies"
      subtitle="Policy rules backed by the admin API with reusable list management primitives."
      [columns]="columns"
      [value]="policies()"
      [loading]="loading()"
      [lazy]="false"
      [rows]="25"
      [totalRecords]="policies().length"
      [globalFilterFields]="['resource', 'action', 'subjectKey', 'description']"
      searchPlaceholder="Search policies"
      emptyTitle="No access policies found"
      emptyMessage="Create your first policy to define authorization rules."
      [actions]="actions"
      (refresh)="loadPolicies()"
      (create)="openCreate()"
      (rowAction)="handleAction($event.actionId, $event.row)"
    />

    <p-dialog
      [visible]="formVisible()"
      [header]="editingPolicyId() ? 'Edit policy' : 'Create policy'"
      [modal]="true"
      [closable]="true"
      [draggable]="false"
      [resizable]="false"
      [style]="{ width: 'min(52rem, 94vw)' }"
      styleClass="surface-dialog"
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
    </p-dialog>

    <app-admin-details-drawer
      [visible]="detailsVisible()"
      [title]="selectedPolicy()?.resource ?? 'Policy details'"
      [items]="detailItems()"
      (visibleChange)="detailsVisible.set($event)"
    />

    <app-admin-confirm-dialog
      [visible]="deleteDialogVisible()"
      title="Delete policy"
      [message]="'Delete ' + (pendingDeletePolicy()?.resource ?? 'this policy') + ' policy?'"
      description="The access rule will be removed from authorization checks. This action cannot be undone."
      confirmLabel="Delete policy"
      [loading]="deleting()"
      (visibleChange)="closeDeleteDialog($event)"
      (confirm)="confirmDelete()"
    />
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AccessPoliciesList {
  private readonly api = inject(AccessPoliciesApiService);
  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly toast = inject(AppToastService);

  protected readonly policies = signal<AccessPolicyAdminDto[]>([]);
  protected readonly loading = signal(false);
  protected readonly saving = signal(false);
  protected readonly formVisible = signal(false);
  protected readonly detailsVisible = signal(false);
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

  protected readonly columns: AdminTableColumn[] = [
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
    { field: 'validTo', header: 'Valid to', cellType: 'date' }
  ];

  protected readonly actions: AdminRowAction<AccessPolicyAdminDto>[] = [
    { id: 'view', label: 'View policy', icon: 'pi pi-eye' },
    { id: 'edit', label: 'Edit policy', icon: 'pi pi-pencil' },
    { id: 'toggle', label: 'Toggle policy', icon: 'pi pi-shield', severity: 'warn' },
    { id: 'delete', label: 'Delete policy', icon: 'pi pi-trash', severity: 'danger' }
  ];

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
      { label: 'Valid to', value: policy.validTo, type: 'date' }
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
    this.loadOptions();
    this.loadPolicies();
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
      .getPolicies()
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe((policies) => this.policies.set(policies));
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
        this.toast.danger('Policy deleted', policy.resource);
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
      isEnabled: value.isEnabled
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
}

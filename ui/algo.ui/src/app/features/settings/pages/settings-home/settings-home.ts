import { ChangeDetectionStrategy, Component, computed, effect, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule, NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import { ButtonModule } from 'primeng/button';
import { InputNumberModule } from 'primeng/inputnumber';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { TagModule } from 'primeng/tag';
import { TextareaModule } from 'primeng/textarea';
import { ToggleSwitchModule } from 'primeng/toggleswitch';

import { AppConfigService } from '../../../../core/config/app-config.service';
import {
  AdminAuthPageConfig,
  AdminAuthPageDesignConfig,
  AdminDirection,
  AdminEnvironment,
  AdminShapeMode,
  AdminTemplateConfig,
  AdminThemeMode
} from '../../../../core/config/admin-template-config.model';
import { Permissions } from '../../../../core/permissions/permission.catalog';
import { PermissionService } from '../../../../core/permissions/permission.service';
import { AppToastService } from '../../../../core/services/app-toast.service';
import { AdminConfirmDialog } from '../../../../shared/components/admin-confirm-dialog/admin-confirm-dialog';
import { AdminDataTable } from '../../../../shared/components/admin-data-table/admin-data-table';
import { AdminFormDialog } from '../../../../shared/components/admin-form-dialog/admin-form-dialog';
import { AdminFormField, AdminRowAction, AdminTableColumn } from '../../../../shared/models/admin-table.model';
import { CustomFieldDefinitionsApiService } from '../../../custom-fields/api/custom-field-definitions-api.service';
import {
  CreateCustomFieldDefinitionCommand,
  CustomFieldDefinition,
  CustomFieldEntity,
  CustomFieldType,
  UpdateCustomFieldDefinitionBody
} from '../../../custom-fields/models/custom-fields.models';
import { StorageSettingsApiService } from '../../../storage/api/storage-settings-api.service';
import { FileScanResult, StorageSettings } from '../../../storage/models/storage.models';
import {
  authButtonStyle,
  authCardStyle,
  authPageBackground
} from '../../../auth/utils/auth-page-style.utils';

@Component({
  selector: 'app-settings-home',
  imports: [
    FormsModule,
    ReactiveFormsModule,
    ButtonModule,
    InputNumberModule,
    InputTextModule,
    SelectModule,
    TagModule,
    TextareaModule,
    ToggleSwitchModule,
    DatePipe,
    AdminDataTable,
    AdminFormDialog,
    AdminConfirmDialog
  ],
  template: `
    <div class="dashboard-grid">
      <section class="surface-card dashboard-section">
        <div class="flex flex-col gap-2 md:flex-row md:items-start md:justify-between">
          <div>
            <div class="eyebrow">Template settings</div>
            <h2 class="m-0 mt-1 text-[18px] font-semibold text-slate-950">Workspace configuration</h2>
            <p class="m-0 mt-1 max-w-3xl text-[12px] text-slate-500">
              Reusable local settings for branding, features, security, notifications, API integration, and layout behavior.
            </p>
          </div>
          <div class="flex flex-wrap gap-2">
            <p-tag [value]="config().environment" severity="info" />
            <p-button label="Reset" icon="pi pi-refresh" severity="secondary" size="small" [outlined]="true" (onClick)="reset()" />
          </div>
        </div>
      </section>

      <section class="grid gap-3 xl:grid-cols-2">
        <article class="surface-card dashboard-section">
          <h3 class="settings-title">General</h3>
          <div class="settings-grid">
            <label class="settings-field">
              <span>App name</span>
              <input pInputText [ngModel]="config().appName" (ngModelChange)="patch({ appName: $event })" />
            </label>
            <label class="settings-field">
              <span>Workspace name</span>
              <input pInputText [ngModel]="config().workspaceName" (ngModelChange)="patch({ workspaceName: $event })" />
            </label>
            <label class="settings-field">
              <span>Environment</span>
              <p-select [options]="environmentOptions" [ngModel]="config().environment" (ngModelChange)="patch({ environment: $event })" appendTo="body" />
            </label>
            <label class="settings-field">
              <span>Timezone</span>
              <input pInputText [ngModel]="config().timezone" (ngModelChange)="patch({ timezone: $event })" />
            </label>
            <label class="settings-field">
              <span>Default language</span>
              <input pInputText [ngModel]="config().defaultLanguage" (ngModelChange)="patch({ defaultLanguage: $event })" />
            </label>
            <label class="settings-field">
              <span>Direction</span>
              <p-select [options]="directionOptions" [ngModel]="config().direction" (ngModelChange)="patch({ direction: $event })" appendTo="body" />
            </label>
          </div>
        </article>

        <article class="surface-card dashboard-section">
          <h3 class="settings-title">Branding</h3>
          <div class="settings-grid">
            <label class="settings-field md:col-span-2">
              <span>Logo placeholder URL</span>
              <input pInputText [ngModel]="config().logoUrl ?? ''" (ngModelChange)="patch({ logoUrl: $event || null })" placeholder="https://..." />
            </label>
            <label class="settings-field">
              <span>Sidebar title</span>
              <input pInputText [ngModel]="config().sidebarTitle" (ngModelChange)="patch({ sidebarTitle: $event })" />
            </label>
            <label class="settings-field">
              <span>Primary color</span>
              <input pInputText type="color" [ngModel]="config().primaryColor" (ngModelChange)="patch({ primaryColor: $event })" />
            </label>
            <label class="settings-field md:col-span-2">
              <span>Favicon placeholder URL</span>
              <input pInputText [ngModel]="config().faviconUrl ?? ''" (ngModelChange)="patch({ faviconUrl: $event || null })" placeholder="https://..." />
            </label>
          </div>
        </article>

        <article class="surface-card dashboard-section xl:col-span-2">
          <div class="flex flex-col gap-2 md:flex-row md:items-start md:justify-between">
            <div>
              <h3 class="settings-title">Auth page designer</h3>
              <p class="m-0 text-[12px] text-slate-500">
                Tune the login and create account screens with live visual feedback.
              </p>
            </div>
            <p-tag value="Live preview" severity="success" />
          </div>

          <div class="mt-4 grid gap-4 xl:grid-cols-[minmax(0,1.25fr)_minmax(24rem,.75fr)]">
            <div class="grid gap-4">
              <div>
                <div class="settings-list-title mb-2">Visual style</div>
                <div class="settings-grid">
                  <label class="settings-field">
                    <span>Background start</span>
                    <input pInputText type="color" [ngModel]="config().authPageDesign.backgroundStart" (ngModelChange)="patchAuthPageDesign({ backgroundStart: $event })" />
                  </label>
                  <label class="settings-field">
                    <span>Background end</span>
                    <input pInputText type="color" [ngModel]="config().authPageDesign.backgroundEnd" (ngModelChange)="patchAuthPageDesign({ backgroundEnd: $event })" />
                  </label>
                  <label class="settings-field">
                    <span>Accent color</span>
                    <input pInputText type="color" [ngModel]="config().authPageDesign.accentColor" (ngModelChange)="patchAuthPageDesign({ accentColor: $event })" />
                  </label>
                  <label class="settings-field">
                    <span>Accent opacity</span>
                    <p-inputnumber [ngModel]="config().authPageDesign.accentOpacity" (ngModelChange)="patchAuthPageDesign({ accentOpacity: $event })" [min]="0" [max]="1" [minFractionDigits]="2" [maxFractionDigits]="2" />
                  </label>
                  <label class="settings-field">
                    <span>Accent size</span>
                    <p-inputnumber [ngModel]="config().authPageDesign.accentSizePercent" (ngModelChange)="patchAuthPageDesign({ accentSizePercent: $event })" suffix="%" [min]="10" [max]="80" />
                  </label>
                  <label class="settings-field">
                    <span>Card background</span>
                    <input pInputText type="color" [ngModel]="config().authPageDesign.cardBackground" (ngModelChange)="patchAuthPageDesign({ cardBackground: $event })" />
                  </label>
                  <label class="settings-field">
                    <span>Card border</span>
                    <input pInputText type="color" [ngModel]="config().authPageDesign.cardBorderColor" (ngModelChange)="patchAuthPageDesign({ cardBorderColor: $event })" />
                  </label>
                  <label class="settings-field">
                    <span>Card radius</span>
                    <p-inputnumber [ngModel]="config().authPageDesign.cardRadiusPx" (ngModelChange)="patchAuthPageDesign({ cardRadiusPx: $event })" suffix=" px" [min]="0" [max]="40" />
                  </label>
                  <label class="settings-field">
                    <span>Login card width</span>
                    <p-inputnumber [ngModel]="config().authPageDesign.loginCardWidthRem" (ngModelChange)="patchAuthPageDesign({ loginCardWidthRem: $event })" suffix=" rem" [min]="20" [max]="44" />
                  </label>
                  <label class="settings-field">
                    <span>Register card width</span>
                    <p-inputnumber [ngModel]="config().authPageDesign.registerCardWidthRem" (ngModelChange)="patchAuthPageDesign({ registerCardWidthRem: $event })" suffix=" rem" [min]="24" [max]="56" />
                  </label>
                  <label class="settings-field">
                    <span>Button background</span>
                    <input pInputText type="color" [ngModel]="config().authPageDesign.buttonBackground" (ngModelChange)="patchAuthPageDesign({ buttonBackground: $event })" />
                  </label>
                  <label class="settings-field">
                    <span>Button text</span>
                    <input pInputText type="color" [ngModel]="config().authPageDesign.buttonTextColor" (ngModelChange)="patchAuthPageDesign({ buttonTextColor: $event })" />
                  </label>
                  <label class="settings-field md:col-span-2">
                    <span>Card shadow CSS</span>
                    <input pInputText [ngModel]="config().authPageDesign.cardShadow" (ngModelChange)="patchAuthPageDesign({ cardShadow: $event })" />
                  </label>
                </div>
              </div>

              <div>
                <div class="settings-list-title mb-2">Copy</div>
                <div class="settings-grid">
                  <label class="settings-field">
                    <span>Auth brand label</span>
                    <input pInputText [ngModel]="config().authPage.brandLabel" (ngModelChange)="patchAuthPage({ brandLabel: $event })" />
                  </label>
                  <label class="settings-field">
                    <span>Login title</span>
                    <input pInputText [ngModel]="config().authPage.loginTitle" (ngModelChange)="patchAuthPage({ loginTitle: $event })" />
                  </label>
                  <label class="settings-field md:col-span-2">
                    <span>Login subtitle</span>
                    <textarea pTextarea rows="2" [ngModel]="config().authPage.loginSubtitle" (ngModelChange)="patchAuthPage({ loginSubtitle: $event })"></textarea>
                  </label>
                  <label class="settings-field">
                    <span>Login button label</span>
                    <input pInputText [ngModel]="config().authPage.loginSubmitLabel" (ngModelChange)="patchAuthPage({ loginSubmitLabel: $event })" />
                  </label>
                  <label class="settings-field">
                    <span>Register prompt</span>
                    <input pInputText [ngModel]="config().authPage.registerPrompt" (ngModelChange)="patchAuthPage({ registerPrompt: $event })" />
                  </label>
                  <label class="settings-field">
                    <span>Register link label</span>
                    <input pInputText [ngModel]="config().authPage.registerLinkLabel" (ngModelChange)="patchAuthPage({ registerLinkLabel: $event })" />
                  </label>
                  <label class="settings-field">
                    <span>Register title</span>
                    <input pInputText [ngModel]="config().authPage.registerTitle" (ngModelChange)="patchAuthPage({ registerTitle: $event })" />
                  </label>
                  <label class="settings-field md:col-span-2">
                    <span>Register subtitle</span>
                    <textarea pTextarea rows="2" [ngModel]="config().authPage.registerSubtitle" (ngModelChange)="patchAuthPage({ registerSubtitle: $event })"></textarea>
                  </label>
                  <label class="settings-field">
                    <span>Register button label</span>
                    <input pInputText [ngModel]="config().authPage.registerSubmitLabel" (ngModelChange)="patchAuthPage({ registerSubmitLabel: $event })" />
                  </label>
                  <label class="settings-field">
                    <span>Register back link label</span>
                    <input pInputText [ngModel]="config().authPage.registerBackLinkLabel" (ngModelChange)="patchAuthPage({ registerBackLinkLabel: $event })" />
                  </label>
                </div>
              </div>
            </div>

            <div class="auth-designer-preview min-h-96 rounded-xl border border-slate-200 p-6" [style.--auth-background-image]="authDesignerPreviewBackground()">
              <div class="mx-auto p-5" [style]="authDesignerPreviewCardStyle()">
                <div class="text-[10px] font-semibold uppercase tracking-[0.2em]" [style.color]="config().authPageDesign.accentColor">
                  {{ config().authPage.brandLabel }}
                </div>
                <div class="mt-3 text-2xl font-semibold text-slate-950">{{ config().authPage.loginTitle }}</div>
                <p class="mt-2 text-xs leading-5 text-slate-500">{{ config().authPage.loginSubtitle }}</p>
                <div class="mt-5 grid gap-3">
                  <div class="h-11 rounded-lg border border-slate-200 bg-white"></div>
                  <div class="h-11 rounded-lg border border-slate-200 bg-white"></div>
                </div>
                <button class="mt-5 h-11 w-full rounded-lg border text-sm font-semibold" [style]="authDesignerPreviewButtonStyle()">
                  {{ config().authPage.loginSubmitLabel }}
                </button>
                <div class="mt-4 text-xs text-slate-500">
                  {{ config().authPage.registerPrompt }}
                  <span class="font-semibold" [style.color]="config().authPageDesign.accentColor">
                    {{ config().authPage.registerLinkLabel }}
                  </span>
                </div>
              </div>
            </div>
          </div>
        </article>

        <article class="surface-card dashboard-section">
          <h3 class="settings-title">Theme</h3>
          <div class="settings-grid">
            <label class="settings-field">
              <span>Mode</span>
              <p-select [options]="themeOptions" [ngModel]="config().theme" (ngModelChange)="patch({ theme: $event })" appendTo="body" />
            </label>
            <label class="settings-field">
              <span>Style</span>
              <p-select [options]="shapeOptions" [ngModel]="config().shape" (ngModelChange)="patch({ shape: $event })" appendTo="body" />
            </label>
            <label class="settings-switch">
              <span>Compact mode</span>
              <p-toggleswitch [ngModel]="config().compactMode" (ngModelChange)="patch({ compactMode: $event })" />
            </label>
            <label class="settings-switch">
              <span>Sidebar collapsed</span>
              <p-toggleswitch [ngModel]="config().sidebarCollapsed" (ngModelChange)="patch({ sidebarCollapsed: $event })" />
            </label>
          </div>
        </article>

        <article class="surface-card dashboard-section">
          <h3 class="settings-title">Security</h3>
          <div class="settings-grid">
            <label class="settings-field">
              <span>Session timeout</span>
              <p-inputnumber [ngModel]="config().sessionTimeoutMinutes" (ngModelChange)="patch({ sessionTimeoutMinutes: $event })" suffix=" min" />
            </label>
            <label class="settings-field">
              <span>Password minimum length</span>
              <p-inputnumber [ngModel]="config().passwordPolicy.minLength" (ngModelChange)="patchPassword({ minLength: $event })" />
            </label>
            <label class="settings-switch"><span>Uppercase required</span><p-toggleswitch [ngModel]="config().passwordPolicy.requireUppercase" (ngModelChange)="patchPassword({ requireUppercase: $event })" /></label>
            <label class="settings-switch"><span>Number required</span><p-toggleswitch [ngModel]="config().passwordPolicy.requireNumber" (ngModelChange)="patchPassword({ requireNumber: $event })" /></label>
            <label class="settings-switch"><span>Symbol required</span><p-toggleswitch [ngModel]="config().passwordPolicy.requireSymbol" (ngModelChange)="patchPassword({ requireSymbol: $event })" /></label>
            <label class="settings-switch"><span>2FA enabled</span><p-toggleswitch [ngModel]="config().twoFactorEnabled" (ngModelChange)="patch({ twoFactorEnabled: $event })" /></label>
            <label class="settings-field md:col-span-2">
              <span>Allowed email domains</span>
              <input pInputText [ngModel]="domainsText()" (ngModelChange)="setDomains($event)" placeholder="example.com, company.dev" />
            </label>
          </div>
        </article>

        <article class="surface-card dashboard-section">
          <h3 class="settings-title">Notifications</h3>
          <div class="settings-grid">
            <label class="settings-switch"><span>Email notifications</span><p-toggleswitch [ngModel]="config().emailNotifications" (ngModelChange)="patch({ emailNotifications: $event })" /></label>
            <label class="settings-switch"><span>System alerts</span><p-toggleswitch [ngModel]="config().systemAlerts" (ngModelChange)="patch({ systemAlerts: $event })" /></label>
            <label class="settings-switch"><span>Error alerts</span><p-toggleswitch [ngModel]="config().errorAlerts" (ngModelChange)="patch({ errorAlerts: $event })" /></label>
          </div>
        </article>

        <article class="surface-card dashboard-section">
          <h3 class="settings-title">API</h3>
          <div class="settings-grid">
            <label class="settings-field md:col-span-2">
              <span>API base URL</span>
              <input pInputText [ngModel]="config().apiBaseUrl" (ngModelChange)="patch({ apiBaseUrl: $event })" />
            </label>
          </div>

          <div class="mt-3 grid gap-2">
            <div class="settings-list-title">API keys</div>
            @for (key of config().apiKeys; track key.id) {
              <div class="settings-list-row">
                <span>{{ key.name }}</span>
                <small>{{ key.createdAt | date: 'mediumDate' }}</small>
              </div>
            }
            <p-button label="Create API key" icon="pi pi-key" size="small" severity="secondary" [outlined]="true" (onClick)="placeholder('API key')" />
          </div>

          <div class="mt-3 grid gap-2">
            <div class="settings-list-title">Webhooks</div>
            @for (webhook of config().webhooks; track webhook.id) {
              <div class="settings-list-row">
                <span>{{ webhook.name }}</span>
                <small>{{ webhook.enabled ? 'Enabled' : 'Disabled' }}</small>
              </div>
            } @empty {
              <div class="settings-list-row"><span>No webhooks configured</span><small>Placeholder</small></div>
            }
            <p-button label="Create webhook" icon="pi pi-send" size="small" severity="secondary" [outlined]="true" (onClick)="placeholder('Webhook')" />
          </div>
        </article>

        <article class="surface-card dashboard-section xl:col-span-2">
          <div class="flex flex-col gap-2 md:flex-row md:items-start md:justify-between">
            <div>
              <h3 class="settings-title">File storage</h3>
              <p class="m-0 text-[12px] text-slate-500">
                Reads Amazon S3 storage settings from the database and lets you manage the scanner flow from one place.
              </p>
            </div>
            <div class="flex flex-wrap gap-2">
              <p-tag value="Amazon S3" severity="contrast" />
              <p-tag [value]="storageSettings()?.source === 'database' ? 'DB-backed' : 'Not loaded'" [severity]="storageSettings() ? 'success' : 'warn'" />
            </div>
          </div>

          <div class="mt-4 settings-grid">
            <label class="settings-field md:col-span-2">
              <span>S3 endpoint URL</span>
              <input pInputText [formControl]="storageForm.controls.endpointUrl" placeholder="https://s3.amazonaws.com" />
            </label>
            <label class="settings-field">
              <span>Access key</span>
              <input pInputText [formControl]="storageForm.controls.accessKey" placeholder="AKIA..." />
            </label>
            <label class="settings-field">
              <span>Secret key</span>
              <input pInputText type="password" [formControl]="storageForm.controls.secretKey" placeholder="Leave blank to keep current secret" />
            </label>
            <label class="settings-field">
              <span>Bucket</span>
              <input pInputText [formControl]="storageForm.controls.bucketName" placeholder="app-files" />
            </label>
            <label class="settings-field">
              <span>Region</span>
              <input pInputText [formControl]="storageForm.controls.region" placeholder="us-east-1" />
            </label>
            <label class="settings-field md:col-span-2">
              <span>Folder / key prefix</span>
              <input pInputText [formControl]="storageForm.controls.folder" placeholder="uploads/documents" />
            </label>
            <label class="settings-switch">
              <span>Use path-style URLs</span>
              <p-toggleswitch [formControl]="storageForm.controls.usePathStyle" />
            </label>
            <div class="settings-list-row md:col-span-2">
              <span>Stored secret</span>
              <small>{{ storageSettings()?.storage?.secretKeyMasked || 'Not available' }}</small>
            </div>
          </div>

          <div class="mt-4 flex flex-wrap gap-2">
            <p-button
              label="Reload storage config"
              icon="pi pi-refresh"
              size="small"
              severity="secondary"
              [outlined]="true"
              [loading]="storageLoading()"
              (onClick)="loadStorageSettings()"
            />
            <p-button
              label="Save storage config"
              icon="pi pi-save"
              size="small"
              [loading]="storageSaving()"
              [disabled]="storageForm.invalid || !canUpdate()"
              (onClick)="saveStorageSettings()"
            />
          </div>

          <div class="mt-6 border-t border-slate-200 pt-4">
            <div class="flex flex-col gap-2 md:flex-row md:items-start md:justify-between">
              <div>
                <div class="settings-list-title">File scanner</div>
                <p class="m-0 text-[12px] text-slate-500">
                  Scan files before they are accepted into the S3 folder.
                </p>
              </div>
              @if (storageSettings()?.updatedAt; as updatedAt) {
                <small class="text-slate-500">Updated {{ updatedAt | date: 'medium' }}</small>
              }
            </div>

            <div class="mt-3 settings-grid">
              <label class="settings-switch">
                <span>Scanner enabled</span>
                <p-toggleswitch [formControl]="storageForm.controls.scannerEnabled" />
              </label>
              <label class="settings-field">
                <span>Scanner provider</span>
                <input pInputText [formControl]="storageForm.controls.scannerProvider" placeholder="clamav" />
              </label>
              <label class="settings-field md:col-span-2">
                <span>Scanner endpoint URL</span>
                <input pInputText [formControl]="storageForm.controls.scannerEndpointUrl" placeholder="https://scanner.internal/api/scan" />
              </label>
              <label class="settings-field">
                <span>Scanner API key</span>
                <input pInputText [formControl]="storageForm.controls.scannerApiKey" placeholder="Read from DB or set a new key" />
              </label>
              <label class="settings-field">
                <span>Quarantine folder</span>
                <input pInputText [formControl]="storageForm.controls.quarantineFolder" placeholder="quarantine" />
              </label>
            </div>

            <div class="mt-4 flex flex-col gap-3 rounded-2xl border border-slate-200 bg-slate-50/70 p-4">
              <div class="flex flex-col gap-2 md:flex-row md:items-center md:justify-between">
                <div>
                  <div class="text-sm font-semibold text-slate-950">Scanner test</div>
                  <div class="text-[12px] text-slate-500">
                    Pick a file and send it to the configured scanner endpoint.
                  </div>
                </div>
                <div class="text-[12px] text-slate-500">
                  {{ selectedScanFileName() || 'No file selected' }}
                </div>
              </div>

              <div class="flex flex-wrap items-center gap-2">
                <label class="dashboard-filter-button flex cursor-pointer items-center gap-2">
                  <i class="pi pi-file"></i>
                  Choose file
                  <input type="file" class="hidden" (change)="onScannerFileSelected($event)" />
                </label>
                <p-button
                  label="Run scan"
                  icon="pi pi-shield"
                  size="small"
                  [loading]="scannerRunning()"
                  [disabled]="!selectedScanFile()"
                  (onClick)="runScanner()"
                />
              </div>

              @if (lastScanResult(); as result) {
                <div class="rounded-2xl border border-slate-200 bg-white p-3">
                  <div class="flex flex-wrap items-center justify-between gap-2">
                    <strong class="text-sm text-slate-950">{{ result.fileName }}</strong>
                    <p-tag [value]="result.status.toUpperCase()" [severity]="scanSeverity(result)" />
                  </div>
                  <div class="mt-2 grid gap-1 text-[12px] text-slate-600">
                    <div>Engine: {{ result.engine }}</div>
                    <div>Message: {{ result.message }}</div>
                    <div>Scanned: {{ result.scannedAt | date: 'medium' }}</div>
                  </div>
                </div>
              }
            </div>
          </div>
        </article>
      </section>

      <section class="surface-card dashboard-section">
        <div class="flex flex-col gap-3 md:flex-row md:items-start md:justify-between">
          <div>
            <div class="eyebrow">Dynamic custom fields</div>
            <h2 class="m-0 mt-1 text-[18px] font-semibold text-slate-950">Entity extensions</h2>
            <p class="m-0 mt-1 max-w-3xl text-[12px] text-slate-500">
              Define schema-free fields for users, roles, and access policies with JSONB-backed metadata, validation, and automatic UI projection.
            </p>
          </div>
          <div class="flex flex-wrap gap-2">
            @for (entity of customFieldEntityOptions; track entity.value) {
              <button
                type="button"
                class="rounded-full px-3 py-1.5 text-xs font-semibold transition"
                [class]="selectedCustomFieldEntity() === entity.value ? 'bg-slate-900 text-white' : 'bg-slate-100 text-slate-600 hover:bg-slate-200'"
                (click)="selectedCustomFieldEntity.set(entity.value)"
              >
                {{ entity.label }}
              </button>
            }
          </div>
        </div>

        <div class="mt-4">
          <app-admin-data-table
            title="Custom fields"
            [subtitle]="customFieldsSubtitle()"
            [columns]="customFieldColumns"
            [value]="customFieldDefinitions()"
            [loading]="customFieldsLoading()"
            [lazy]="false"
            [rows]="10"
            [totalRecords]="customFieldDefinitions().length"
            [globalFilterFields]="['key', 'label', 'type']"
            [showCreate]="canUpdate()"
            createLabel="Add field"
            [actions]="customFieldActions()"
            (refresh)="loadCustomFieldDefinitions()"
            (create)="openCustomFieldCreate()"
            (rowAction)="handleCustomFieldAction($event.actionId, $event.row)"
          />
        </div>
      </section>
    </div>

    <app-admin-form-dialog
      [visible]="customFieldFormVisible()"
      [title]="editingCustomFieldId() ? 'Edit custom field' : 'Create custom field'"
      [form]="customFieldForm"
      [fields]="customFieldFormFields()"
      [submitLabel]="editingCustomFieldId() ? 'Save changes' : 'Create field'"
      [loading]="customFieldSaving()"
      (visibleChange)="closeCustomFieldForm($event)"
      (submit)="saveCustomField()"
    />

    <app-admin-confirm-dialog
      [visible]="customFieldDeleteVisible()"
      title="Delete custom field"
      [message]="'Delete ' + (pendingCustomFieldDelete()?.label ?? 'this field') + '?'"
      description="This removes the field definition and its managed indexes. Stored JSON values remain in existing records until you clean them up separately."
      confirmLabel="Delete field"
      [loading]="customFieldDeleting()"
      (visibleChange)="closeCustomFieldDeleteDialog($event)"
      (confirm)="confirmDeleteCustomField()"
    />
  `,
  styles: [`
    :host .auth-designer-preview {
      background-image: var(--auth-background-image);
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SettingsHome {
  private readonly configService = inject(AppConfigService);
  private readonly customFieldDefinitionsApi = inject(CustomFieldDefinitionsApiService);
  private readonly storageSettingsApi = inject(StorageSettingsApiService);
  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly permissionService = inject(PermissionService);
  private readonly toast = inject(AppToastService);

  protected readonly config = this.configService.config;
  protected readonly domainsText = computed(() => this.config().allowedEmailDomains.join(', '));
  protected readonly canUpdate = computed(() => this.permissionService.can({ any: [Permissions.settings.update] }));
  protected readonly selectedCustomFieldEntity = signal<CustomFieldEntity>('users');
  protected readonly customFieldDefinitions = signal<CustomFieldDefinition[]>([]);
  protected readonly customFieldsLoading = signal(false);
  protected readonly customFieldFormVisible = signal(false);
  protected readonly customFieldSaving = signal(false);
  protected readonly customFieldDeleting = signal(false);
  protected readonly customFieldDeleteVisible = signal(false);
  protected readonly editingCustomFieldId = signal<string | null>(null);
  protected readonly pendingCustomFieldDelete = signal<CustomFieldDefinition | null>(null);
  protected readonly storageSettings = signal<StorageSettings | null>(null);
  protected readonly storageLoading = signal(false);
  protected readonly storageSaving = signal(false);
  protected readonly selectedScanFile = signal<File | null>(null);
  protected readonly selectedScanFileName = computed(() => this.selectedScanFile()?.name ?? '');
  protected readonly scannerRunning = signal(false);
  protected readonly lastScanResult = signal<FileScanResult | null>(null);

  protected readonly environmentOptions = optionList<AdminEnvironment>(['Dev', 'Staging', 'Prod']);
  protected readonly directionOptions = optionList<AdminDirection>(['ltr', 'rtl']);
  protected readonly themeOptions = optionList<AdminThemeMode>(['light', 'dark']);
  protected readonly shapeOptions = optionList<AdminShapeMode>(['rounded', 'sharp']);
  protected readonly customFieldEntityOptions = [
    { label: 'Users', value: 'users' as const },
    { label: 'Roles', value: 'roles' as const },
    { label: 'Access policies', value: 'accessPolicies' as const },
    { label: 'Products', value: 'products' as const },
    { label: 'Orders', value: 'orders' as const }
  ];
  protected readonly customFieldTypeOptions = [
    { label: 'Text', value: 'text' as const },
    { label: 'Number', value: 'number' as const },
    { label: 'Boolean', value: 'boolean' as const },
    { label: 'Date', value: 'date' as const },
    { label: 'Select', value: 'select' as const },
    { label: 'Multi-select', value: 'multiSelect' as const },
    { label: 'JSON', value: 'json' as const }
  ];
  protected readonly customFieldColumns: AdminTableColumn[] = [
    { field: 'key', header: 'Key', sortable: true, filter: true },
    { field: 'label', header: 'Label', sortable: true, filter: true },
    { field: 'type', header: 'Type', sortable: true, filter: true },
    { field: 'required', header: 'Required', cellType: 'boolean', filter: true, filterType: 'boolean' },
    { field: 'searchable', header: 'Search', cellType: 'boolean' },
    { field: 'filterable', header: 'Filter', cellType: 'boolean' },
    { field: 'sortable', header: 'Sort', cellType: 'boolean' },
    { field: 'updatedAt', header: 'Updated', sortable: true, cellType: 'date' }
  ];
  protected readonly customFieldsSubtitle = computed(
    () => `Definitions for ${this.selectedCustomFieldEntity()} are stored once and projected into forms, tables, details, and JSONB-backed payloads.`
  );
  protected readonly customFieldActions = computed<AdminRowAction<CustomFieldDefinition>[]>(() => {
    if (!this.canUpdate()) {
      return [];
    }

    return [
      { id: 'edit', label: 'Edit field', icon: 'pi pi-pencil' },
      { id: 'delete', label: 'Delete field', icon: 'pi pi-trash', severity: 'danger' }
    ];
  });

  protected readonly customFieldForm = this.formBuilder.group({
    entity: ['users' as CustomFieldEntity, Validators.required],
    key: ['', Validators.required],
    label: ['', Validators.required],
    type: ['text' as CustomFieldType, Validators.required],
    required: [false],
    searchable: [false],
    filterable: [false],
    sortable: [false],
    visibleInTable: [true],
    visibleInForm: [true],
    visibleInDetails: [true],
    optionsJson: [''],
    defaultValueJson: [''],
    validationJson: ['']
  });
  protected readonly storageForm = this.formBuilder.group({
    endpointUrl: ['', Validators.required],
    accessKey: ['', Validators.required],
    secretKey: [''],
    bucketName: ['', Validators.required],
    region: ['', Validators.required],
    folder: ['', Validators.required],
    usePathStyle: [false],
    scannerEnabled: [true],
    scannerProvider: ['clamav', Validators.required],
    scannerEndpointUrl: [''],
    scannerApiKey: [''],
    quarantineFolder: ['']
  });

  protected readonly customFieldFormFields = computed<AdminFormField[]>(() => [
    {
      key: 'entity',
      label: 'Entity',
      type: 'select',
      options: this.customFieldEntityOptions
    },
    { key: 'key', label: 'Key', type: 'text', required: true },
    { key: 'label', label: 'Label', type: 'text', required: true },
    {
      key: 'type',
      label: 'Type',
      type: 'select',
      required: true,
      options: this.customFieldTypeOptions
    },
    { key: 'required', label: 'Required', type: 'switch' },
    { key: 'searchable', label: 'Searchable', type: 'switch' },
    { key: 'filterable', label: 'Filterable', type: 'switch' },
    { key: 'sortable', label: 'Sortable', type: 'switch' },
    { key: 'visibleInTable', label: 'Visible in table', type: 'switch' },
    { key: 'visibleInForm', label: 'Visible in form', type: 'switch' },
    { key: 'visibleInDetails', label: 'Visible in details', type: 'switch' },
    { key: 'optionsJson', label: 'Options JSON', type: 'json' },
    { key: 'defaultValueJson', label: 'Default value JSON', type: 'json' },
    { key: 'validationJson', label: 'Validation JSON', type: 'json' }
  ]);

  constructor() {
    effect(() => {
      this.loadCustomFieldDefinitions();
    });

    this.loadStorageSettings();
  }

  protected patch(patch: Partial<AdminTemplateConfig>): void {
    if (!this.canUpdate()) {
      return;
    }
    this.configService.update(patch);
  }

  protected patchPassword(patch: Partial<AdminTemplateConfig['passwordPolicy']>): void {
    if (!this.canUpdate()) {
      return;
    }
    this.configService.update({ passwordPolicy: patch as AdminTemplateConfig['passwordPolicy'] });
  }

  protected patchAuthPage(patch: Partial<AdminAuthPageConfig>): void {
    if (!this.canUpdate()) {
      return;
    }
    this.configService.update({ authPage: patch as AdminAuthPageConfig });
  }

  protected patchAuthPageDesign(patch: Partial<AdminAuthPageDesignConfig>): void {
    if (!this.canUpdate()) {
      return;
    }
    this.configService.update({ authPageDesign: patch as AdminAuthPageDesignConfig });
  }

  protected authDesignerPreviewBackground(): string {
    return authPageBackground(this.config().authPageDesign);
  }

  protected authDesignerPreviewCardStyle(): Record<string, string> {
    return {
      ...authCardStyle(this.config().authPageDesign, 24),
      width: '100%',
      borderStyle: 'solid',
      borderWidth: '1px'
    };
  }

  protected authDesignerPreviewButtonStyle(): Record<string, string> {
    return authButtonStyle(this.config().authPageDesign);
  }

  protected setDomains(value: string): void {
    if (!this.canUpdate()) {
      return;
    }
    this.configService.update({
      allowedEmailDomains: value
        .split(',')
        .map((domain) => domain.trim())
        .filter(Boolean)
    });
  }

  protected placeholder(label: string): void {
    this.toast.info(`${label} placeholder`, 'Wire this to your backend when the endpoint is available.');
  }

  protected loadCustomFieldDefinitions(): void {
    this.customFieldsLoading.set(true);
    this.customFieldDefinitionsApi
      .getDefinitions(this.selectedCustomFieldEntity())
      .pipe(finalize(() => this.customFieldsLoading.set(false)))
      .subscribe((definitions) => this.customFieldDefinitions.set(definitions));
  }

  protected openCustomFieldCreate(): void {
    if (!this.canUpdate()) {
      return;
    }

    this.editingCustomFieldId.set(null);
    this.customFieldForm.enable({ emitEvent: false });
    this.customFieldForm.reset({
      entity: this.selectedCustomFieldEntity(),
      key: '',
      label: '',
      type: 'text',
      required: false,
      searchable: false,
      filterable: false,
      sortable: false,
      visibleInTable: true,
      visibleInForm: true,
      visibleInDetails: true,
      optionsJson: '',
      defaultValueJson: '',
      validationJson: ''
    });
    this.customFieldFormVisible.set(true);
  }

  protected handleCustomFieldAction(actionId: string, definition: CustomFieldDefinition): void {
    if (!this.canUpdate()) {
      return;
    }

    switch (actionId) {
      case 'edit':
        this.editingCustomFieldId.set(definition.id);
        this.customFieldForm.reset({
          entity: definition.entity,
          key: definition.key,
          label: definition.label,
          type: definition.type,
          required: definition.required,
          searchable: definition.searchable,
          filterable: definition.filterable,
          sortable: definition.sortable,
          visibleInTable: definition.visibleInTable,
          visibleInForm: definition.visibleInForm,
          visibleInDetails: definition.visibleInDetails,
          optionsJson: this.stringifyJson(definition.options),
          defaultValueJson: this.stringifyJson(definition.defaultValue),
          validationJson: this.stringifyJson(definition.validation)
        });
        this.customFieldForm.controls.entity.disable({ emitEvent: false });
        this.customFieldForm.controls.key.disable({ emitEvent: false });
        this.customFieldFormVisible.set(true);
        break;
      case 'delete':
        this.pendingCustomFieldDelete.set(definition);
        this.customFieldDeleteVisible.set(true);
        break;
    }
  }

  protected closeCustomFieldForm(visible: boolean): void {
    this.customFieldFormVisible.set(visible);
    if (!visible) {
      this.editingCustomFieldId.set(null);
    }
  }

  protected saveCustomField(): void {
    if (!this.canUpdate() || this.customFieldForm.invalid || this.customFieldSaving()) {
      return;
    }

    const value = this.customFieldForm.getRawValue();

    let options: unknown[] | null;
    let defaultValue: unknown;
    let validation: Record<string, unknown> | null;

    try {
      options = this.parseOptionalJson(value.optionsJson) as unknown[] | null;
      defaultValue = this.parseOptionalJson(value.defaultValueJson);
      validation = this.parseOptionalJson(value.validationJson) as Record<string, unknown> | null;
    } catch (error) {
      this.toast.error('Invalid JSON', error instanceof Error ? error.message : 'Check the custom field JSON inputs.');
      return;
    }

    this.customFieldSaving.set(true);

    const request = {
      entity: value.entity,
      key: value.key.trim(),
      label: value.label.trim(),
      type: value.type,
      required: value.required,
      searchable: value.searchable,
      filterable: value.filterable,
      sortable: value.sortable,
      visibleInTable: value.visibleInTable,
      visibleInForm: value.visibleInForm,
      visibleInDetails: value.visibleInDetails,
      options,
      defaultValue,
      validation
    };

    const saveRequest = this.editingCustomFieldId()
      ? this.customFieldDefinitionsApi.updateDefinition(
          this.editingCustomFieldId()!,
          request as UpdateCustomFieldDefinitionBody
        )
      : this.customFieldDefinitionsApi.createDefinition(request as CreateCustomFieldDefinitionCommand);

    saveRequest
      .pipe(finalize(() => this.customFieldSaving.set(false)))
      .subscribe(() => {
        this.toast.success(
          this.editingCustomFieldId() ? 'Custom field updated' : 'Custom field created',
          request.label
        );
        this.customFieldFormVisible.set(false);
        this.loadCustomFieldDefinitions();
      });
  }

  protected closeCustomFieldDeleteDialog(visible: boolean): void {
    this.customFieldDeleteVisible.set(visible);
    if (!visible && !this.customFieldDeleting()) {
      this.pendingCustomFieldDelete.set(null);
    }
  }

  protected confirmDeleteCustomField(): void {
    const definition = this.pendingCustomFieldDelete();
    if (!definition || !this.canUpdate() || this.customFieldDeleting()) {
      return;
    }

    this.customFieldDeleting.set(true);
    this.customFieldDefinitionsApi
      .deleteDefinition(definition.id)
      .pipe(finalize(() => this.customFieldDeleting.set(false)))
      .subscribe(() => {
        this.toast.success('Custom field deleted', definition.label);
        this.customFieldDeleteVisible.set(false);
        this.pendingCustomFieldDelete.set(null);
        this.loadCustomFieldDefinitions();
      });
  }

  protected reset(): void {
    if (!this.canUpdate()) {
      return;
    }
    this.configService.reset();
    this.toast.success('Settings reset', 'Template defaults restored.');
  }

  protected loadStorageSettings(): void {
    this.storageLoading.set(true);
    this.storageSettingsApi
      .getSettings()
      .pipe(finalize(() => this.storageLoading.set(false)))
      .subscribe({
        next: (settings) => {
          this.storageSettings.set(settings);
          this.storageForm.reset({
            endpointUrl: settings.storage.endpointUrl,
            accessKey: settings.storage.accessKey,
            secretKey: '',
            bucketName: settings.storage.bucketName,
            region: settings.storage.region,
            folder: settings.storage.folder,
            usePathStyle: settings.storage.usePathStyle,
            scannerEnabled: settings.scanner.enabled,
            scannerProvider: settings.scanner.provider,
            scannerEndpointUrl: settings.scanner.endpointUrl ?? '',
            scannerApiKey: settings.scanner.apiKey ?? '',
            quarantineFolder: settings.scanner.quarantineFolder ?? ''
          });
        },
        error: () => {
          this.toast.error('Storage settings unavailable', 'Could not load S3 and file scanner settings from the database.');
        }
      });
  }

  protected saveStorageSettings(): void {
    if (!this.canUpdate() || this.storageForm.invalid || this.storageSaving()) {
      return;
    }

    const value = this.storageForm.getRawValue();

    this.storageSaving.set(true);
    this.storageSettingsApi
      .updateSettings({
        storage: {
          endpointUrl: value.endpointUrl.trim(),
          accessKey: value.accessKey.trim(),
          secretKey: value.secretKey.trim(),
          bucketName: value.bucketName.trim(),
          region: value.region.trim(),
          folder: value.folder.trim(),
          usePathStyle: value.usePathStyle
        },
        scanner: {
          enabled: value.scannerEnabled,
          provider: value.scannerProvider.trim(),
          endpointUrl: value.scannerEndpointUrl.trim() || null,
          apiKey: value.scannerApiKey.trim() || null,
          quarantineFolder: value.quarantineFolder.trim() || null
        }
      })
      .pipe(finalize(() => this.storageSaving.set(false)))
      .subscribe((settings) => {
        this.storageSettings.set(settings);
        this.storageForm.controls.secretKey.setValue('');
        this.toast.success('Storage settings saved', 'Amazon S3 and scanner configuration updated from the database record.');
      });
  }

  protected onScannerFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement | null;
    const file = input?.files?.item(0) ?? null;

    this.selectedScanFile.set(file);
    this.lastScanResult.set(null);
  }

  protected runScanner(): void {
    const file = this.selectedScanFile();
    if (!file || this.scannerRunning()) {
      return;
    }

    this.scannerRunning.set(true);
    this.storageSettingsApi
      .scanFile(file)
      .pipe(finalize(() => this.scannerRunning.set(false)))
      .subscribe({
        next: (result) => {
          this.lastScanResult.set(result);
          this.toast.success('File scanned', `${result.fileName} returned ${result.status}.`);
        },
        error: () => {
          this.toast.error('Scan failed', 'The file scanner endpoint did not accept the file.');
        }
      });
  }

  protected scanSeverity(result: FileScanResult): 'success' | 'danger' | 'warn' | 'secondary' {
    switch (result.status) {
      case 'clean':
        return 'success';
      case 'infected':
        return 'danger';
      case 'pending':
        return 'warn';
      default:
        return 'secondary';
    }
  }

  private parseOptionalJson(value: string): unknown {
    if (!value.trim()) {
      return null;
    }

    return JSON.parse(value);
  }

  private stringifyJson(value: unknown): string {
    if (value === null || value === undefined || value === '') {
      return '';
    }

    return JSON.stringify(value, null, 2);
  }
}

function optionList<TValue extends string>(values: readonly TValue[]): { label: string; value: TValue }[] {
  return values.map((value) => ({
    label: value.toUpperCase(),
    value
  }));
}

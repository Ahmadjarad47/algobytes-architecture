import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { InputNumberModule } from 'primeng/inputnumber';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { TagModule } from 'primeng/tag';
import { TextareaModule } from 'primeng/textarea';
import { ToggleSwitchModule } from 'primeng/toggleswitch';

import { AppConfigService } from '../../../../core/config/app-config.service';
import {
  AdminDirection,
  AdminEnvironment,
  AdminShapeMode,
  AdminTemplateConfig,
  AdminThemeMode
} from '../../../../core/config/admin-template-config.model';
import { Permissions } from '../../../../core/permissions/permission.catalog';
import { PermissionService } from '../../../../core/permissions/permission.service';
import { AppToastService } from '../../../../core/services/app-toast.service';

@Component({
  selector: 'app-settings-home',
  imports: [
    FormsModule,
    ButtonModule,
    InputNumberModule,
    InputTextModule,
    SelectModule,
    TagModule,
    TextareaModule,
    ToggleSwitchModule,
    DatePipe
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
      </section>
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SettingsHome {
  private readonly configService = inject(AppConfigService);
  private readonly permissionService = inject(PermissionService);
  private readonly toast = inject(AppToastService);

  protected readonly config = this.configService.config;
  protected readonly domainsText = computed(() => this.config().allowedEmailDomains.join(', '));
  protected readonly canUpdate = computed(() => this.permissionService.can({ any: [Permissions.settings.update] }));

  protected readonly environmentOptions = optionList<AdminEnvironment>(['Dev', 'Staging', 'Prod']);
  protected readonly directionOptions = optionList<AdminDirection>(['ltr', 'rtl']);
  protected readonly themeOptions = optionList<AdminThemeMode>(['light', 'dark']);
  protected readonly shapeOptions = optionList<AdminShapeMode>(['rounded', 'sharp']);

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

  protected reset(): void {
    if (!this.canUpdate()) {
      return;
    }
    this.configService.reset();
    this.toast.success('Settings reset', 'Template defaults restored.');
  }
}

function optionList<TValue extends string>(values: readonly TValue[]): { label: string; value: TValue }[] {
  return values.map((value) => ({
    label: value.toUpperCase(),
    value
  }));
}

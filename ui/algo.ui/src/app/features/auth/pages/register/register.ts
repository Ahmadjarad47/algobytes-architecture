import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  FormControl,
  NonNullableFormBuilder,
  ReactiveFormsModule,
  Validators
} from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { DatePickerModule } from 'primeng/datepicker';
import { FloatLabelModule } from 'primeng/floatlabel';
import { FluidModule } from 'primeng/fluid';
import { InputNumberModule } from 'primeng/inputnumber';
import { InputTextModule } from 'primeng/inputtext';
import { MultiSelectModule } from 'primeng/multiselect';
import { PasswordModule } from 'primeng/password';
import { SelectModule } from 'primeng/select';
import { TextareaModule } from 'primeng/textarea';
import { ToggleSwitchModule } from 'primeng/toggleswitch';

import { AppToastService } from '../../../../core/services/app-toast.service';
import { AppConfigService } from '../../../../core/config/app-config.service';
import { AuthFacadeService } from '../../services/auth-facade.service';
import { AuthApiService } from '../../api/auth-api.service';
import { CustomFieldDefinition } from '../../../custom-fields/models/custom-fields.models';
import {
  customFieldControlKey,
  customFieldInitialValues,
  customFieldsPayload
} from '../../../custom-fields/utils/custom-field.utils';
import {
  authButtonStyle,
  authCardStyle,
  authPageBackground
} from '../../utils/auth-page-style.utils';

@Component({
  selector: 'app-register',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink,
    ButtonModule,
    CardModule,
    DatePickerModule,
    FloatLabelModule,
    FluidModule,
    InputNumberModule,
    InputTextModule,
    MultiSelectModule,
    PasswordModule,
    SelectModule,
    TextareaModule,
    ToggleSwitchModule
  ],
  template: `
    <main class="auth-page-shell grid min-h-dvh place-items-center px-4" [style.--auth-background-image]="authBackground()">
      <p-card styleClass="w-full border backdrop-blur" [style]="authCardStyle()">
        <div class="mb-8">
          <div class="text-xs font-semibold uppercase tracking-[0.22em]" [style.color]="authDesign().accentColor">{{ authPage().brandLabel }}</div>
          <h1 class="m-0 mt-3 text-3xl font-semibold text-surface-950">{{ authPage().registerTitle }}</h1>
          <p class="m-0 mt-2 text-sm text-surface-500">
            {{ authPage().registerSubtitle }}
          </p>
        </div>

        <form [formGroup]="form" class="grid gap-5 md:grid-cols-2" (ngSubmit)="submit()">
          <p-fluid>
            <p-floatlabel variant="on">
              <input pInputText formControlName="displayName" class="w-full" />
              <label>Display name</label>
            </p-floatlabel>
          </p-fluid>

          <p-fluid>
            <p-floatlabel variant="on">
              <input pInputText type="email" formControlName="email" class="w-full" />
              <label>Email</label>
            </p-floatlabel>
          </p-fluid>

          <p-fluid>
            <p-floatlabel variant="on">
              <p-password
                formControlName="password"
                [feedback]="false"
                [toggleMask]="true"
                inputStyleClass="w-full"
                styleClass="w-full"
              />
              <label>Password</label>
            </p-floatlabel>
          </p-fluid>

          @for (definition of customFieldDefinitions(); track definition.key) {
            @let key = customFieldKey(definition);
            <p-fluid [class]="definition.type === 'json' ? 'md:col-span-2' : ''">
              @if (definition.type === 'boolean') {
                <label class="flex min-h-12 items-center justify-between rounded-md border border-surface-300 px-3 py-2">
                  <span class="text-sm font-medium text-surface-700">
                    {{ definition.label }}{{ definition.required ? ' *' : '' }}
                  </span>
                  <p-toggleswitch [formControl]="control(key)" />
                </label>
              } @else {
                <p-floatlabel variant="on">
                  @switch (definition.type) {
                    @case ('number') {
                      <p-inputnumber [formControl]="control(key)" inputStyleClass="w-full" />
                    }
                    @case ('date') {
                      <p-datepicker
                        [formControl]="control(key)"
                        [showIcon]="true"
                        appendTo="body"
                        styleClass="w-full"
                        inputStyleClass="w-full"
                      />
                    }
                    @case ('select') {
                      <p-select
                        [formControl]="control(key)"
                        [options]="fieldOptions(definition)"
                        optionLabel="label"
                        optionValue="value"
                        appendTo="body"
                        class="w-full"
                      />
                    }
                    @case ('multiSelect') {
                      <p-multiselect
                        [formControl]="control(key)"
                        [options]="fieldOptions(definition)"
                        optionLabel="label"
                        optionValue="value"
                        appendTo="body"
                        class="w-full"
                      />
                    }
                    @case ('json') {
                      <textarea
                        pTextarea
                        [formControl]="control(key)"
                        rows="4"
                        class="w-full resize-y font-mono text-xs"
                      ></textarea>
                    }
                    @default {
                      <input pInputText [formControl]="control(key)" class="w-full" />
                    }
                  }
                  <label>{{ definition.label }}{{ definition.required ? ' *' : '' }}</label>
                </p-floatlabel>
              }
            </p-fluid>
          }

          <p-fluid>
            <p-floatlabel variant="on">
              <p-password
                formControlName="confirmPassword"
                [feedback]="false"
                [toggleMask]="true"
                inputStyleClass="w-full"
                styleClass="w-full"
              />
              <label>Confirm password</label>
            </p-floatlabel>
          </p-fluid>

          @if (statusMessage()) {
            <div class="md:col-span-2 rounded-2xl border border-surface-200 bg-surface-50 px-4 py-3 text-sm text-surface-700">
              {{ statusMessage() }}
            </div>
          }

          <div class="md:col-span-2 flex items-center justify-between gap-4">
            <a routerLink="/auth/login" class="text-sm font-semibold text-surface-700 no-underline">
              {{ authPage().registerBackLinkLabel }}
            </a>

            <p-button
              type="submit"
              [label]="authPage().registerSubmitLabel"
              [style]="authButtonStyle()"
              [loading]="submitting()"
              [disabled]="form.invalid || submitting()"
            />
          </div>
        </form>
      </p-card>
    </main>
  `,
  styles: [`
    :host .auth-page-shell {
      background-image: var(--auth-background-image);
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class Register {
  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly authApi = inject(AuthApiService);
  private readonly authFacade = inject(AuthFacadeService);
  private readonly toast = inject(AppToastService);
  private readonly configService = inject(AppConfigService);

  protected readonly form = this.formBuilder.group({
    displayName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
    confirmPassword: ['', Validators.required]
  });

  protected readonly submitting = signal(false);
  protected readonly statusMessage = signal('');
  protected readonly customFieldDefinitions = signal<CustomFieldDefinition[]>([]);
  protected readonly authPage = this.configService.authPage;
  protected readonly authDesign = this.configService.authPageDesign;

  constructor() {
    this.authApi.getRegistrationFields().subscribe({
      next: (definitions) => {
        this.customFieldDefinitions.set(definitions);
        this.syncCustomFieldControls(definitions);
      },
      error: () => {
        this.customFieldDefinitions.set([]);
      }
    });
  }

  protected submit(): void {
    if (this.form.invalid || this.submitting()) {
      return;
    }

    this.submitting.set(true);
    this.statusMessage.set('');
    const value = this.form.getRawValue() as Record<string, unknown>;

    this.authFacade
      .register({
        displayName: String(value['displayName'] ?? ''),
        email: String(value['email'] ?? ''),
        password: String(value['password'] ?? ''),
        confirmPassword: String(value['confirmPassword'] ?? ''),
        customFields: customFieldsPayload(this.customFieldDefinitions(), value)
      })
      .pipe(finalize(() => this.submitting.set(false)))
      .subscribe({
        next: (response) => {
          this.statusMessage.set(response.message);
          this.toast.success('Account created', response.message);
          this.form.reset({
            displayName: '',
            email: response.email,
            password: '',
            confirmPassword: '',
            ...customFieldInitialValues(this.customFieldDefinitions(), null)
          });
        },
        error: () => {
          this.statusMessage.set('Registration could not be completed right now.');
        }
      });
  }

  protected authBackground(): string {
    return authPageBackground(this.authDesign());
  }

  protected authCardStyle(): Record<string, string> {
    return authCardStyle(this.authDesign(), this.authDesign().registerCardWidthRem);
  }

  protected authButtonStyle(): Record<string, string> {
    return authButtonStyle(this.authDesign());
  }

  protected customFieldKey(definition: CustomFieldDefinition): string {
    return customFieldControlKey(definition);
  }

  protected control(key: string): FormControl {
    return this.form.get(key) as FormControl;
  }

  protected fieldOptions(definition: CustomFieldDefinition): { label: string; value: string }[] {
    return Array.isArray(definition.options)
      ? definition.options.map((option) => this.toFieldOption(option))
      : [];
  }

  private syncCustomFieldControls(definitions: readonly CustomFieldDefinition[]): void {
    const initialValues = customFieldInitialValues(definitions, null);
    for (const definition of definitions) {
      const key = customFieldControlKey(definition);
      if (this.form.contains(key as never)) {
        continue;
      }

      this.form.addControl(
        key as never,
        new FormControl(initialValues[key] ?? null, definition.required ? Validators.required : null) as never
      );
    }
  }

  private toFieldOption(option: unknown): { label: string; value: string } {
    if (typeof option === 'object' && option !== null && !Array.isArray(option)) {
      const record = option as Record<string, unknown>;
      const value = record['value'] === undefined || record['value'] === null
        ? String(record['label'] ?? '')
        : String(record['value']);

      return {
        label: record['label'] === undefined || record['label'] === null ? value : String(record['label']),
        value
      };
    }

    return {
      label: String(option),
      value: String(option)
    };
  }
}

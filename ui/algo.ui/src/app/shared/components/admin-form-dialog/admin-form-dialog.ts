import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { DatePickerModule } from 'primeng/datepicker';
import { DrawerModule } from 'primeng/drawer';
import { FloatLabelModule } from 'primeng/floatlabel';
import { FluidModule } from 'primeng/fluid';
import { InputNumberModule } from 'primeng/inputnumber';
import { InputTextModule } from 'primeng/inputtext';
import { MultiSelectModule } from 'primeng/multiselect';
import { PasswordModule } from 'primeng/password';
import { SelectModule } from 'primeng/select';
import { TextareaModule } from 'primeng/textarea';
import { ToggleSwitchModule } from 'primeng/toggleswitch';

import { AdminFormField } from '../../models/admin-table.model';

@Component({
  selector: 'app-admin-form-dialog',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    ButtonModule,
    DatePickerModule,
    DrawerModule,
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
    <p-drawer
      position="right"
      [visible]="visible()"
      [header]="title()"
      [modal]="true"
      [dismissible]="true"
      [blockScroll]="true"
      [appendTo]="'body'"
      [style]="{ width: 'min(38rem, 100vw)' }"
      styleClass="surface-dialog app-details-drawer"
      [maskStyle]="{ background: 'rgba(15, 23, 42, 0.18)', backdropFilter: 'none' }"
      (visibleChange)="visibleChange.emit($event)"
    >
      <form [formGroup]="form()" class="flex flex-col gap-4" (ngSubmit)="submit.emit()">
        <p-fluid>
          <div class="grid gap-3 md:grid-cols-2">
            @for (field of fields(); track field.key) {
              <div [class]="field.type === 'textarea' ? 'md:col-span-2' : ''">
                @if (field.type === 'switch') {
                  <label class="flex items-center justify-between rounded-lg border border-surface-200 px-3 py-2.5">
                    <span class="text-sm font-medium text-surface-700">{{ field.label }}</span>
                    <p-toggleswitch [formControl]="control(field.key)" />
                  </label>
                } @else {
                  <p-floatlabel variant="on">
                    @switch (field.type) {
                      @case ('email') {
                        <input pInputText type="email" [formControl]="control(field.key)" class="w-full" />
                      }
                      @case ('password') {
                        <p-password
                          [formControl]="control(field.key)"
                          [feedback]="false"
                          [toggleMask]="true"
                          inputStyleClass="w-full"
                          styleClass="w-full"
                        />
                      }
                      @case ('textarea') {
                        <textarea
                          pTextarea
                          [formControl]="control(field.key)"
                          rows="3"
                          class="w-full resize-y"
                        ></textarea>
                      }
                      @case ('json') {
                        <textarea
                          pTextarea
                          [formControl]="control(field.key)"
                          rows="5"
                          class="w-full resize-y font-mono text-xs"
                        ></textarea>
                      }
                      @case ('number') {
                        <p-inputnumber [formControl]="control(field.key)" inputStyleClass="w-full" />
                      }
                      @case ('date') {
                        <p-datepicker
                          [formControl]="control(field.key)"
                          [showIcon]="true"
                          appendTo="body"
                          styleClass="w-full"
                          inputStyleClass="w-full"
                        />
                      }
                      @case ('select') {
                        <p-select
                          [formControl]="control(field.key)"
                          [options]="field.options ?? []"
                          optionLabel="label"
                          optionValue="value"
                          appendTo="body"
                          class="w-full"
                        />
                      }
                      @case ('multiselect') {
                        <p-multiselect
                          [formControl]="control(field.key)"
                          [options]="field.options ?? []"
                          optionLabel="label"
                          optionValue="value"
                          appendTo="body"
                          class="w-full"
                        />
                      }
                      @default {
                        <input pInputText [formControl]="control(field.key)" class="w-full" />
                      }
                    }
                    <label>{{ field.label }}{{ field.required ? ' *' : '' }}</label>
                  </p-floatlabel>
                }
              </div>
            }
            <ng-content select="[adminFormExtras]" />
          </div>
        </p-fluid>

        <div class="flex justify-end gap-2 border-t border-surface-200 pt-3">
          <p-button
            label="Cancel"
            severity="secondary"
            size="small"
            [outlined]="true"
            type="button"
            (onClick)="visibleChange.emit(false)"
          />
          <p-button
            [label]="submitLabel()"
            size="small"
            type="submit"
            [disabled]="form().invalid || loading()"
            [loading]="loading()"
          />
        </div>
      </form>
    </p-drawer>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AdminFormDialog {
  readonly visible = input(false);
  readonly title = input.required<string>();
  readonly form = input.required<FormGroup>();
  readonly fields = input<AdminFormField[]>([]);
  readonly submitLabel = input('Save');
  readonly loading = input(false);

  readonly visibleChange = output<boolean>();
  readonly submit = output<void>();

  control(key: string): FormControl {
    return this.form().get(key) as FormControl;
  }
}

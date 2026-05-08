import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  NonNullableFormBuilder,
  ReactiveFormsModule,
  Validators
} from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { FloatLabelModule } from 'primeng/floatlabel';
import { FluidModule } from 'primeng/fluid';
import { InputTextModule } from 'primeng/inputtext';
import { PasswordModule } from 'primeng/password';

import { AppToastService } from '../../../../core/services/app-toast.service';
import { AuthFacadeService } from '../../services/auth-facade.service';

@Component({
  selector: 'app-register',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink,
    ButtonModule,
    CardModule,
    FloatLabelModule,
    FluidModule,
    InputTextModule,
    PasswordModule
  ],
  template: `
    <main class="grid min-h-dvh place-items-center bg-[radial-gradient(circle_at_top_left,_rgba(14,165,233,0.16),_transparent_30%),linear-gradient(180deg,_#f8fafc,_#eef2f7)] px-4">
      <p-card styleClass="w-full max-w-xl rounded-3xl border border-white/70 bg-white/90 shadow-xl backdrop-blur">
        <div class="mb-8">
          <div class="text-xs font-semibold uppercase tracking-[0.22em] text-sky-700">algo.ui</div>
          <h1 class="m-0 mt-3 text-3xl font-semibold text-surface-950">Create an account</h1>
          <p class="m-0 mt-2 text-sm text-surface-500">
            Register a new operator account for the dashboard workspace.
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
              Back to sign in
            </a>

            <p-button
              type="submit"
              label="Create account"
              [loading]="submitting()"
              [disabled]="form.invalid || submitting()"
            />
          </div>
        </form>
      </p-card>
    </main>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class Register {
  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly authFacade = inject(AuthFacadeService);
  private readonly toast = inject(AppToastService);

  protected readonly form = this.formBuilder.group({
    displayName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
    confirmPassword: ['', Validators.required]
  });

  protected readonly submitting = signal(false);
  protected readonly statusMessage = signal('');

  protected submit(): void {
    if (this.form.invalid || this.submitting()) {
      return;
    }

    this.submitting.set(true);
    this.statusMessage.set('');

    this.authFacade
      .register(this.form.getRawValue())
      .pipe(finalize(() => this.submitting.set(false)))
      .subscribe({
        next: (response) => {
          this.statusMessage.set(response.message);
          this.toast.success('Account created', response.message);
          this.form.reset({
            displayName: '',
            email: response.email,
            password: '',
            confirmPassword: ''
          });
        },
        error: () => {
          this.statusMessage.set('Registration could not be completed right now.');
        }
      });
  }
}

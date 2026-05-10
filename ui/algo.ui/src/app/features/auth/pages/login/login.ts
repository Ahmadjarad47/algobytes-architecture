import { ChangeDetectionStrategy, Component, effect, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  NonNullableFormBuilder,
  ReactiveFormsModule,
  Validators
} from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { FloatLabelModule } from 'primeng/floatlabel';
import { FluidModule } from 'primeng/fluid';
import { InputTextModule } from 'primeng/inputtext';
import { PasswordModule } from 'primeng/password';

import { Permissions } from '../../../../core/permissions/permission.catalog';
import { PermissionService } from '../../../../core/permissions/permission.service';
import { AppToastService } from '../../../../core/services/app-toast.service';
import { LoginResponseDto } from '../../models/auth.models';
import { AuthFacadeService } from '../../services/auth-facade.service';

@Component({
  selector: 'app-login',
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
    <main class="grid min-h-dvh place-items-center bg-[radial-gradient(circle_at_top_left,_rgba(132,204,22,0.18),_transparent_30%),linear-gradient(180deg,_#f8fafc,_#eef2f7)] px-4">
      <p-card styleClass="w-full max-w-md rounded-3xl border border-white/70 bg-white/90 shadow-xl backdrop-blur">
        <div class="mb-8">
          <div class="text-xs font-semibold uppercase tracking-[0.22em] text-lime-700">algo.ui</div>
          <h1 class="m-0 mt-3 text-3xl font-semibold text-surface-950">Welcome back</h1>
          <p class="m-0 mt-2 text-sm text-surface-500">
            Sign in to manage users, roles, policies, and operational logs.
          </p>
        </div>

        <form [formGroup]="form" class="flex flex-col gap-5" (ngSubmit)="submit()">
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

          @if (totpChallenge()) {
            <div class="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm text-slate-700">
              {{ totpChallenge()?.message }}
            </div>

            @if (totpChallenge()?.setupUri) {
              <div class="rounded-2xl border border-slate-200 bg-white p-4 text-center">
                <img
                  class="mx-auto h-40 w-40 rounded-lg border border-slate-200 p-2"
                  [src]="qrCodeUrl()"
                  alt="TOTP setup QR code"
                />
                <div class="mt-3 text-xs text-slate-500">Setup key</div>
                <div class="mt-1 font-mono text-sm text-slate-900">{{ totpChallenge()?.setupKey }}</div>
              </div>
            }

            <p-fluid>
              <p-floatlabel variant="on">
                <input pInputText type="text" formControlName="totpCode" maxlength="6" class="w-full" />
                <label>Authenticator code</label>
              </p-floatlabel>
            </p-fluid>
          }

          @if (errorMessage()) {
            <div class="rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
              {{ errorMessage() }}
            </div>
          }

          <p-button
            type="submit"
            label="Sign in"
            [fluid]="true"
            [loading]="submitting()"
            [disabled]="form.invalid || submitting()"
          />
        </form>

        <div class="mt-6 text-sm text-surface-500">
          New here?
          <a routerLink="/auth/register" class="font-semibold text-surface-900 no-underline">
            Create an account
          </a>
        </div>
      </p-card>
    </main>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class Login {
  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly authFacade = inject(AuthFacadeService);
  private readonly router = inject(Router);
  private readonly toast = inject(AppToastService);
  private readonly permissionService = inject(PermissionService);

  protected readonly form = this.formBuilder.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
    totpCode: ['']
  });

  protected readonly submitting = signal(false);
  protected readonly errorMessage = signal('');
  protected readonly totpChallenge = signal<LoginResponseDto['totpChallenge']>(null);
  protected readonly qrCodeUrl = signal<string>('');

  constructor() {
    effect(() => {
      const hasChallenge = this.totpChallenge() !== null;
      const passwordControl = this.form.controls.password;

      if (hasChallenge && passwordControl.enabled) {
        passwordControl.disable({ emitEvent: false });
      } else if (!hasChallenge && passwordControl.disabled) {
        passwordControl.enable({ emitEvent: false });
      }
    });
  }

  protected submit(): void {
    if (this.form.invalid || this.submitting()) {
      return;
    }

    this.submitting.set(true);
    this.errorMessage.set('');

    this.authFacade
      .login({
        email: this.form.controls.email.value,
        password: this.form.controls.password.value,
        totpCode: this.form.controls.totpCode.value?.trim() || undefined
      })
      .pipe(finalize(() => this.submitting.set(false)))
      .subscribe({
        next: (response) => {
          if (response.totpChallenge) {
            this.totpChallenge.set(response.totpChallenge);
            this.qrCodeUrl.set(
              response.totpChallenge.setupUri
                ? `https://api.qrserver.com/v1/create-qr-code/?size=220x220&data=${encodeURIComponent(response.totpChallenge.setupUri)}`
                : ''
            );
            return;
          }

          this.totpChallenge.set(null);
          this.qrCodeUrl.set('');
          this.toast.success('Signed in', 'Welcome back.');
          void this.router.navigateByUrl(this.getLandingRoute());
        },
        error: () => {
          this.errorMessage.set('Unable to sign in. Please verify your credentials and backend availability.');
        }
      });
  }

  private getLandingRoute(): string {
    if (this.permissionService.can({ any: [Permissions.users.read] })) {
      return '/users';
    }

    if (this.permissionService.can({ any: [Permissions.roles.read] })) {
      return '/roles';
    }

    if (this.permissionService.can({ any: [Permissions.accessPolicies.read] })) {
      return '/access-policies';
    }

    if (this.permissionService.can({ any: [Permissions.sessions.read] })) {
      return '/active-sessions';
    }

    if (this.permissionService.can({ any: [Permissions.logs.read] })) {
      return '/logs';
    }

    if (this.permissionService.can({ any: [Permissions.errorLogs.read] })) {
      return '/error-logs';
    }

    if (this.permissionService.can({ any: [Permissions.settings.read] })) {
      return '/settings';
    }

    if (this.permissionService.can({ any: [Permissions.reports.read] })) {
      return '/reports';
    }

    return '/access-denied';
  }
}

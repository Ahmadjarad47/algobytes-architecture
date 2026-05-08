import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
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

import { AppToastService } from '../../../../core/services/app-toast.service';
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

  protected readonly form = this.formBuilder.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required]
  });

  protected readonly submitting = signal(false);
  protected readonly errorMessage = signal('');

  protected submit(): void {
    if (this.form.invalid || this.submitting()) {
      return;
    }

    this.submitting.set(true);
    this.errorMessage.set('');

    this.authFacade
      .login(this.form.getRawValue())
      .pipe(finalize(() => this.submitting.set(false)))
      .subscribe({
        next: () => {
          this.toast.success('Signed in', 'Welcome back.');
          void this.router.navigateByUrl('/dashboard');
        },
        error: () => {
          this.errorMessage.set('Unable to sign in. Please verify your credentials and backend availability.');
        }
      });
  }
}

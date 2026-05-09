import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ButtonModule } from 'primeng/button';

@Component({
  selector: 'app-access-denied',
  imports: [RouterLink, ButtonModule],
  template: `
    <section class="surface-card mx-auto mt-8 max-w-xl rounded-2xl p-6 text-center">
      <div class="text-xs font-semibold uppercase tracking-wide text-slate-500">Access control</div>
      <h1 class="m-0 mt-2 text-2xl font-semibold text-slate-950">Access denied</h1>
      <p class="mx-auto mt-2 max-w-md text-sm text-slate-500">
        You do not have permission to view this page. Contact an administrator if you need access.
      </p>
      <div class="mt-5 flex items-center justify-center gap-2">
        <p-button label="Go to dashboard" [routerLink]="['/dashboard']" />
      </div>
    </section>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AccessDenied {}

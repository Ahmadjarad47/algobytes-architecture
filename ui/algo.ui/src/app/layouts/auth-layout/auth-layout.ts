import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-auth-layout',
  imports: [RouterOutlet],
  template: `
    <main class="min-h-dvh bg-surface-50 text-surface-950">
      <router-outlet />
    </main>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AuthLayout {}

import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { ButtonModule } from 'primeng/button';

export type AdminStateKind =
  | 'loading'
  | 'empty'
  | 'no-results'
  | 'error'
  | 'denied'
  | 'api-unavailable';

@Component({
  selector: 'app-admin-state',
  imports: [ButtonModule],
  template: `
    <div class="flex flex-col items-center gap-2 px-6 py-10 text-center">
      <i [class]="icon()" class="text-2xl text-surface-300"></i>
      <h3 class="m-0 text-sm font-semibold text-surface-700">{{ title() }}</h3>
      <p class="m-0 max-w-md text-xs text-surface-500">{{ message() }}</p>
      @if (actionLabel()) {
        <p-button
          [label]="actionLabel()"
          icon="pi pi-refresh"
          size="small"
          severity="secondary"
          [outlined]="true"
          (onClick)="action.emit()"
        />
      }
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AdminState {
  readonly kind = input<AdminStateKind>('empty');
  readonly title = input('No data');
  readonly message = input('There is nothing to show yet.');
  readonly actionLabel = input('');
  readonly action = output<void>();

  icon(): string {
    switch (this.kind()) {
      case 'loading':
        return 'pi pi-spin pi-spinner';
      case 'error':
      case 'api-unavailable':
        return 'pi pi-exclamation-triangle';
      case 'denied':
        return 'pi pi-lock';
      case 'no-results':
        return 'pi pi-search';
      default:
        return 'pi pi-inbox';
    }
  }
}

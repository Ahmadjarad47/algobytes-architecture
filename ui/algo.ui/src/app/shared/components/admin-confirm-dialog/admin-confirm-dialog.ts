import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';

@Component({
  selector: 'app-admin-confirm-dialog',
  imports: [CommonModule, ButtonModule, DialogModule],
  template: `
    <p-dialog
      [visible]="visible()"
      [header]="title()"
      [modal]="true"
      [closable]="!loading()"
      [draggable]="false"
      [resizable]="false"
      [style]="{ width: 'min(30rem, 92vw)' }"
      styleClass="surface-dialog"
      maskStyleClass="backdrop-blur-[3px]"
      (visibleChange)="visibleChange.emit($event)"
    >
      <div class="flex gap-3">
        <div
          class="flex h-10 w-10 flex-none items-center justify-center rounded-xl bg-rose-50 text-rose-600"
        >
          <i class="pi pi-exclamation-triangle text-sm"></i>
        </div>

        <div class="min-w-0">
          <p class="m-0 text-sm font-semibold text-surface-950">{{ message() }}</p>
          @if (description()) {
            <p class="m-0 mt-1 text-xs leading-5 text-surface-500">{{ description() }}</p>
          }
        </div>
      </div>

      <div class="mt-5 flex justify-end gap-2 border-t border-surface-200 pt-3">
        <p-button
          [label]="cancelLabel()"
          severity="secondary"
          size="small"
          [outlined]="true"
          type="button"
          [disabled]="loading()"
          (onClick)="visibleChange.emit(false)"
        />
        <p-button
          [label]="confirmLabel()"
          severity="danger"
          size="small"
          type="button"
          [loading]="loading()"
          (onClick)="confirm.emit()"
        />
      </div>
    </p-dialog>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AdminConfirmDialog {
  readonly visible = input(false);
  readonly title = input('Confirm delete');
  readonly message = input('Delete this record?');
  readonly description = input('This action cannot be undone.');
  readonly confirmLabel = input('Delete');
  readonly cancelLabel = input('Cancel');
  readonly loading = input(false);

  readonly visibleChange = output<boolean>();
  readonly confirm = output<void>();
}

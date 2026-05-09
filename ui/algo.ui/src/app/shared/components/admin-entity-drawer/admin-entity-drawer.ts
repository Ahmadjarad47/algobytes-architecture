import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { ButtonModule } from 'primeng/button';
import { DrawerModule } from 'primeng/drawer';

export type AdminEntityDrawerMode = 'view' | 'create' | 'edit';

@Component({
  selector: 'app-admin-entity-drawer',
  imports: [ButtonModule, DrawerModule],
  template: `
    <p-drawer
      position="right"
      [visible]="visible()"
      [header]="title()"
      [modal]="true"
      [dismissible]="true"
      [blockScroll]="true"
      [appendTo]="'body'"
      [autoZIndex]="true"
      [baseZIndex]="1450"
      [style]="{ width: 'min(42rem, 100vw)' }"
      [maskStyle]="{ background: 'rgba(15, 23, 42, 0.18)', backdropFilter: 'none' }"
      styleClass="surface-dialog app-details-drawer"
      (visibleChange)="visibleChange.emit($event)"
    >
      <div class="flex min-h-full flex-col gap-3 density-compact">
        @if (loading()) {
          <div class="rounded-xl border border-surface-200 bg-surface-50 px-3 py-3 text-sm text-surface-500">
            Loading...
          </div>
        }

        @if (error()) {
          <div class="rounded-xl border border-red-200 bg-red-50 px-3 py-3 text-sm text-red-700">
            {{ error() }}
          </div>
        }

        <div class="min-w-0 flex-1">
          <ng-content />
        </div>

        <div class="sticky bottom-0 -mx-3 mt-auto flex flex-wrap justify-end gap-2 border-t border-surface-200 bg-white/95 px-3 py-3">
          @if (mode() === 'view') {
            @if (showDelete()) {
              <p-button
                label="Delete"
                icon="pi pi-trash"
                severity="danger"
                size="small"
                [outlined]="true"
                [loading]="deleting()"
                (onClick)="delete.emit()"
              />
            }
            <p-button
              label="Close"
              icon="pi pi-times"
              severity="secondary"
              size="small"
              [outlined]="true"
              (onClick)="visibleChange.emit(false)"
            />
          } @else {
            <p-button
              label="Cancel"
              severity="secondary"
              size="small"
              [outlined]="true"
              type="button"
              (onClick)="visibleChange.emit(false)"
            />
            @if (showDelete()) {
              <p-button
                label="Delete"
                icon="pi pi-trash"
                severity="danger"
                size="small"
                [outlined]="true"
                [loading]="deleting()"
                type="button"
                (onClick)="delete.emit()"
              />
            }
            <p-button
              label="Save"
              icon="pi pi-check"
              size="small"
              [disabled]="saveDisabled()"
              [loading]="saving()"
              (onClick)="save.emit()"
            />
          }
        </div>
      </div>
    </p-drawer>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AdminEntityDrawer {
  readonly visible = input(false);
  readonly title = input.required<string>();
  readonly mode = input<AdminEntityDrawerMode>('view');
  readonly loading = input(false);
  readonly saving = input(false);
  readonly deleting = input(false);
  readonly saveDisabled = input(false);
  readonly error = input('');
  readonly showDelete = input(false);

  readonly visibleChange = output<boolean>();
  readonly save = output<void>();
  readonly delete = output<void>();
}

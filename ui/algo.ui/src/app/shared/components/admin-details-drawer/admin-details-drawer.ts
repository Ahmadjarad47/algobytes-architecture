import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { ButtonModule } from 'primeng/button';
import { DrawerModule } from 'primeng/drawer';
import { TagModule } from 'primeng/tag';

import { AdminDetailItem } from '../../models/admin-table.model';

@Component({
  selector: 'app-admin-details-drawer',
  imports: [CommonModule, ButtonModule, DrawerModule, TagModule, DatePipe],
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
      [baseZIndex]="1400"
      [style]="{ width: 'min(30rem, 100vw)' }"
      [maskStyle]="{ background: 'rgba(15, 23, 42, 0.18)', backdropFilter: 'none' }"
      styleClass="surface-dialog app-details-drawer"
      (visibleChange)="visibleChange.emit($event)"
    >
      <div class="app-details-drawer__body density-compact">
        @for (item of items(); track item.label) {
          <section class="app-details-drawer__item">
            <div class="app-details-drawer__label">
              {{ item.label }}
            </div>

            @switch (item.type ?? 'text') {
              @case ('date') {
                <div class="app-details-drawer__value">
                  {{ asDateValue(item.value) | date: 'medium' }}
                </div>
              }
              @case ('json') {
                <pre class="app-details-drawer__json">{{ formatJson(item.value) }}</pre>
              }
              @case ('list') {
                <div class="app-details-drawer__value">{{ formatList(item.value) }}</div>
              }
              @case ('status') {
                <p-tag [value]="formatValue(item.value)" [severity]="item.severity ?? 'secondary'" />
              }
              @default {
                <div class="app-details-drawer__value">{{ formatValue(item.value) }}</div>
              }
            }
          </section>
        } @empty {
          <section class="app-details-drawer__empty">
            No details available for this record.
          </section>
        }

        @if (showCopy() || secondaryCopyLabel() || actionLabel() || secondaryActionLabel()) {
          <div class="sticky bottom-0 -mx-3 mt-2 flex flex-wrap justify-end gap-2 border-t border-surface-200 bg-white/95 px-3 py-3">
            @if (showCopy()) {
              <p-button [label]="copyLabel()" icon="pi pi-copy" severity="secondary" size="small" [outlined]="true" (onClick)="copy.emit()" />
            }
            @if (secondaryCopyLabel()) {
              <p-button [label]="secondaryCopyLabel()" icon="pi pi-copy" severity="secondary" size="small" [outlined]="true" (onClick)="secondaryCopy.emit()" />
            }
            @if (actionLabel()) {
              <p-button [label]="actionLabel()" [icon]="actionIcon()" size="small" (onClick)="action.emit()" />
            }
            @if (secondaryActionLabel()) {
              <p-button [label]="secondaryActionLabel()" [icon]="secondaryActionIcon()" severity="warn" size="small" [outlined]="true" (onClick)="secondaryAction.emit()" />
            }
          </div>
        }
      </div>
    </p-drawer>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AdminDetailsDrawer {
  readonly visible = input(false);
  readonly title = input.required<string>();
  readonly items = input<AdminDetailItem[]>([]);
  readonly showCopy = input(false);
  readonly copyLabel = input('Copy');
  readonly secondaryCopyLabel = input('');
  readonly actionLabel = input('');
  readonly actionIcon = input('pi pi-check');
  readonly secondaryActionLabel = input('');
  readonly secondaryActionIcon = input('pi pi-users');

  readonly visibleChange = output<boolean>();
  readonly copy = output<void>();
  readonly secondaryCopy = output<void>();
  readonly action = output<void>();
  readonly secondaryAction = output<void>();

  formatValue(value: unknown): string {
    if (value === null || value === undefined || value === '') {
      return '-';
    }

    return String(value);
  }

  formatJson(value: unknown): string {
    if (value === null || value === undefined || value === '') {
      return '-';
    }

    return typeof value === 'string' ? value : JSON.stringify(value, null, 2);
  }

  formatList(value: unknown): string {
    if (!Array.isArray(value) || value.length === 0) {
      return '-';
    }

    return value.join(', ');
  }

  asDateValue(value: unknown): string | number | Date | null | undefined {
    if (
      value === null ||
      value === undefined ||
      typeof value === 'string' ||
      typeof value === 'number' ||
      value instanceof Date
    ) {
      return value;
    }

    return undefined;
  }
}

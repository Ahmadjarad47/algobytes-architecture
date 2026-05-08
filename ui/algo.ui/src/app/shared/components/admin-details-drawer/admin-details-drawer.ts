import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { DrawerModule } from 'primeng/drawer';
import { TagModule } from 'primeng/tag';

import { AdminDetailItem } from '../../models/admin-table.model';

@Component({
  selector: 'app-admin-details-drawer',
  imports: [CommonModule, DrawerModule, TagModule, DatePipe],
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
      </div>
    </p-drawer>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AdminDetailsDrawer {
  readonly visible = input(false);
  readonly title = input.required<string>();
  readonly items = input<AdminDetailItem[]>([]);

  readonly visibleChange = output<boolean>();

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

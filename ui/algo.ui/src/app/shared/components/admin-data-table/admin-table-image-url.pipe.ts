import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'tableImageUrl',
  pure: true
})
export class AdminTableImageUrlPipe implements PipeTransform {
  transform(value: unknown, baseUrl?: string): string | null {
    if (typeof value !== 'string') {
      return null;
    }

    const raw = value.trim();
    if (!raw) {
      return null;
    }

    if (/^https?:\/\//i.test(raw) || raw.startsWith('data:') || raw.startsWith('blob:')) {
      return raw;
    }

    if (!baseUrl || !baseUrl.trim()) {
      return raw;
    }

    const normalizedBase = baseUrl.replace(/\/+$/, '');
    const normalizedPath = raw.replace(/^\/+/, '');
    return `${normalizedBase}/${normalizedPath}`;
  }
}

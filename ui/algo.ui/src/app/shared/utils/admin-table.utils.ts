import { TableLazyLoadEvent } from 'primeng/table';

export interface TableQueryState {
  readonly pageNumber: number;
  readonly pageSize: number;
  readonly search?: string;
  readonly sortField?: string;
  readonly sortDirection?: 'Ascending' | 'Descending';
  readonly filters?: Record<string, unknown>;
}

export function toTableQuery(
  event: TableLazyLoadEvent,
  fallbackRows = 25
): TableQueryState {
  const globalValue = readFilterValue(event.filters?.['global']);

  return {
    pageNumber: Math.floor((event.first ?? 0) / (event.rows ?? fallbackRows)) + 1,
    pageSize: event.rows ?? fallbackRows,
    search: typeof globalValue === 'string' ? globalValue : undefined,
    sortField: typeof event.sortField === 'string' ? event.sortField : undefined,
    sortDirection:
      event.sortOrder === -1
        ? 'Descending'
        : event.sortOrder === 1
          ? 'Ascending'
          : undefined,
    filters: flattenFilters(event.filters)
  };
}

function flattenFilters(
  filters: TableLazyLoadEvent['filters']
): Record<string, unknown> {
  const output: Record<string, unknown> = {};

  if (!filters) {
    return output;
  }

  for (const [key, value] of Object.entries(filters)) {
    if (key === 'global') {
      continue;
    }

    const resolvedValue = readFilterValue(value);
    if (resolvedValue !== undefined && resolvedValue !== null && resolvedValue !== '') {
      output[key] = resolvedValue;
    }
  }

  return output;
}

function readFilterValue(value: unknown): unknown {
  if (Array.isArray(value)) {
    return value[0]?.value;
  }

  if (typeof value === 'object' && value && 'value' in value) {
    return (value as { value?: unknown }).value;
  }

  return value;
}

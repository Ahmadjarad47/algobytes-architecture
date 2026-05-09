export type ExportRow = Record<string, unknown>;

export function exportJson(fileName: string, rows: readonly ExportRow[]): void {
  download(`${fileName}.json`, JSON.stringify(rows, null, 2), 'application/json');
}

export function exportCsv(fileName: string, rows: readonly ExportRow[]): void {
  const columns = Array.from(new Set(rows.flatMap((row) => Object.keys(row))));
  const csv = [
    columns.join(','),
    ...rows.map((row) => columns.map((column) => escapeCsv(row[column])).join(','))
  ].join('\n');

  download(`${fileName}.csv`, csv, 'text/csv;charset=utf-8');
}

export function downloadCsvTemplate(fileName: string, columns: readonly string[]): void {
  download(`${fileName}.csv`, `${columns.join(',')}\n`, 'text/csv;charset=utf-8');
}

function escapeCsv(value: unknown): string {
  if (value === null || value === undefined) {
    return '';
  }

  const normalized = Array.isArray(value)
    ? value.join('; ')
    : typeof value === 'object'
      ? JSON.stringify(value)
      : String(value);

  return /[",\n\r]/.test(normalized) ? `"${normalized.replace(/"/g, '""')}"` : normalized;
}

function download(fileName: string, content: string, type: string): void {
  const blob = new Blob([content], { type });
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');

  link.href = url;
  link.download = fileName;
  link.click();
  URL.revokeObjectURL(url);
}

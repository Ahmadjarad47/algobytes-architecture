export interface AdminTableOption {
  readonly label: string;
  readonly value: string | number | boolean;
}

export interface AdminTableColumn {
  readonly field: string;
  readonly header: string;
  readonly sortable?: boolean;
  readonly filter?: boolean;
  readonly filterType?: 'text' | 'numeric' | 'date' | 'boolean';
  readonly widthClass?: string;
  readonly cellType?: 'text' | 'date' | 'boolean' | 'status' | 'json' | 'list';
  readonly placeholder?: string;
  readonly severityMap?: Record<string, 'success' | 'info' | 'warn' | 'danger' | 'secondary' | 'contrast'>;
}

export interface AdminRowAction<TData> {
  readonly id: string;
  readonly label: string;
  readonly icon: string;
  readonly severity?: 'secondary' | 'info' | 'success' | 'warn' | 'danger' | 'contrast';
  readonly disabled?: (row: TData) => boolean;
}

export interface AdminDetailItem {
  readonly label: string;
  readonly value: unknown;
  readonly type?: 'text' | 'date' | 'json' | 'list' | 'status';
  readonly severity?: 'success' | 'info' | 'warn' | 'danger' | 'secondary' | 'contrast';
}

export interface AdminFormFieldOption {
  readonly label: string;
  readonly value: string | number | boolean;
}

export interface AdminFormField {
  readonly key: string;
  readonly label: string;
  readonly type:
    | 'text'
    | 'email'
    | 'password'
    | 'textarea'
    | 'number'
    | 'switch'
    | 'date'
    | 'select'
    | 'multiselect';
  readonly placeholder?: string;
  readonly options?: AdminFormFieldOption[];
}

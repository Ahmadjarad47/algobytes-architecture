export type CustomFieldEntity = 'users' | 'roles' | 'accessPolicies' | 'products' | 'orders';

export type CustomFieldType =
  | 'text'
  | 'number'
  | 'boolean'
  | 'date'
  | 'select'
  | 'multiSelect'
  | 'json';

export interface CustomFieldDefinition {
  readonly id: string;
  readonly entity: CustomFieldEntity;
  readonly key: string;
  readonly label: string;
  readonly type: CustomFieldType;
  readonly required: boolean;
  readonly searchable: boolean;
  readonly filterable: boolean;
  readonly sortable: boolean;
  readonly visibleInTable: boolean;
  readonly visibleInForm: boolean;
  readonly visibleInDetails: boolean;
  readonly options: unknown[] | null;
  readonly defaultValue: unknown;
  readonly validation: Record<string, unknown> | null;
  readonly createdAt: string;
  readonly updatedAt: string;
}

export interface CreateCustomFieldDefinitionCommand {
  readonly entity: CustomFieldEntity;
  readonly key: string;
  readonly label: string;
  readonly type: CustomFieldType;
  readonly required: boolean;
  readonly searchable: boolean;
  readonly filterable: boolean;
  readonly sortable: boolean;
  readonly visibleInTable: boolean;
  readonly visibleInForm: boolean;
  readonly visibleInDetails: boolean;
  readonly options: unknown[] | null;
  readonly defaultValue: unknown;
  readonly validation: Record<string, unknown> | null;
}

export interface UpdateCustomFieldDefinitionBody {
  readonly label: string;
  readonly type: CustomFieldType;
  readonly required: boolean;
  readonly searchable: boolean;
  readonly filterable: boolean;
  readonly sortable: boolean;
  readonly visibleInTable: boolean;
  readonly visibleInForm: boolean;
  readonly visibleInDetails: boolean;
  readonly options: unknown[] | null;
  readonly defaultValue: unknown;
  readonly validation: Record<string, unknown> | null;
}

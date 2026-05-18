import { AdminDetailItem, AdminFormField, AdminTableColumn } from '../../../shared/models/admin-table.model';
import { CustomFieldDefinition } from '../models/custom-fields.models';

const CUSTOM_FIELD_PREFIX = 'customField__';

export function customFieldControlKey(definition: Pick<CustomFieldDefinition, 'key'>): string {
  return `${CUSTOM_FIELD_PREFIX}${definition.key}`;
}

export function customFieldColumns(definitions: readonly CustomFieldDefinition[]): AdminTableColumn[] {
  return definitions
    .filter((definition) => definition.visibleInTable)
    .map((definition) => ({
      field: `customFields.${definition.key}`,
      header: definition.label,
      sortable: definition.sortable,
      filter: definition.filterable,
      filterType: definition.type === 'number' ? 'numeric' : definition.type === 'boolean' ? 'boolean' : definition.type === 'date' ? 'date' : 'text',
      cellType:
        definition.type === 'boolean'
          ? 'boolean'
          : definition.type === 'date'
            ? 'date'
            : definition.type === 'multiSelect'
              ? 'list'
              : definition.type === 'json'
                ? 'json'
                : 'text'
    }));
}

export function customFieldFormFields(definitions: readonly CustomFieldDefinition[]): AdminFormField[] {
  return definitions
    .filter((definition) => definition.visibleInForm)
    .map((definition) => ({
      key: customFieldControlKey(definition),
      label: definition.label,
      type:
        definition.type === 'number'
          ? 'number'
          : definition.type === 'boolean'
            ? 'switch'
            : definition.type === 'date'
              ? 'date'
              : definition.type === 'select'
                ? 'select'
                : definition.type === 'multiSelect'
                  ? 'multiselect'
                  : definition.type === 'json'
                    ? 'json'
                    : 'text',
      required: definition.required,
      options: Array.isArray(definition.options)
        ? definition.options.map((option) => ({
            label: String(option),
            value: String(option)
          }))
        : undefined
    }));
}

export function customFieldDetailItems(
  definitions: readonly CustomFieldDefinition[],
  customFields: Record<string, unknown> | null | undefined
): AdminDetailItem[] {
  if (!customFields) {
    return [];
  }

  return definitions
    .filter((definition) => definition.visibleInDetails && customFields[definition.key] !== undefined)
    .map((definition) => ({
      label: definition.label,
      value: customFields[definition.key],
      type:
        definition.type === 'date'
          ? 'date'
          : definition.type === 'multiSelect'
            ? 'list'
            : definition.type === 'json'
              ? 'json'
              : definition.type === 'boolean'
                ? 'status'
                : 'text',
      severity:
        definition.type === 'boolean'
          ? customFields[definition.key]
            ? 'success'
            : 'secondary'
          : undefined
    }));
}

export function customFieldInitialValues(
  definitions: readonly CustomFieldDefinition[],
  customFields: Record<string, unknown> | null | undefined
): Record<string, unknown> {
  return Object.fromEntries(
    definitions.map((definition) => [
      customFieldControlKey(definition),
      toInitialValue(definition, customFields?.[definition.key] ?? definition.defaultValue)
    ])
  );
}

export function customFieldsPayload(
  definitions: readonly CustomFieldDefinition[],
  formValue: Record<string, unknown>
): Record<string, unknown> {
  return Object.fromEntries(
    definitions
      .map((definition) => [definition.key, normalizeCustomFieldValue(definition.type, formValue[customFieldControlKey(definition)])] as const)
      .filter(([, value]) => value !== null && value !== undefined && value !== '')
  );
}

function normalizeCustomFieldValue(type: CustomFieldDefinition['type'], value: unknown): unknown {
  if (type === 'date') {
    if (value instanceof Date) {
      return value.toISOString();
    }

    return value || null;
  }

  if (type === 'json' && typeof value === 'string' && value.trim()) {
    return JSON.parse(value);
  }

  if (type === 'multiSelect' && !Array.isArray(value)) {
    return [];
  }

  return value;
}

function toInitialValue(definition: CustomFieldDefinition, value: unknown): unknown {
  if (value === undefined || value === null || value === '') {
    return definition.type === 'multiSelect'
      ? []
      : definition.type === 'boolean'
        ? false
        : definition.type === 'json'
          ? ''
          : null;
  }

  if (definition.type === 'date' && typeof value === 'string') {
    return new Date(value);
  }

  if (definition.type === 'json') {
    return typeof value === 'string' ? value : JSON.stringify(value, null, 2);
  }

  return value;
}

import { AccessPolicyConditionField } from './access-policies.models';
import {
  ConditionBuilderState,
  ConditionJsonMappingResult,
  ConditionMode,
  ConditionRow
} from './condition-builder.models';

interface JsonCondition {
  readonly field?: unknown;
  readonly operator?: unknown;
  readonly value?: unknown;
  readonly all?: unknown;
  readonly any?: unknown;
}

export function createEmptyConditionState(): ConditionBuilderState {
  return {
    mode: 'none',
    rows: [],
    advancedJson: '',
    advancedReason: null
  };
}

export function parseConditionJson(
  conditionJson: string | null | undefined,
  fields: readonly AccessPolicyConditionField[]
): ConditionJsonMappingResult {
  if (!conditionJson?.trim()) {
    return toMappingResult(createEmptyConditionState(), fields);
  }

  try {
    const parsed = JSON.parse(conditionJson) as JsonCondition;
    const mode = Array.isArray(parsed.all) ? 'all' : Array.isArray(parsed.any) ? 'any' : null;
    const items = mode ? (parsed[mode] as unknown[]) : null;

    if (!mode || !items || Object.keys(parsed).some((key) => key !== mode)) {
      return advanced(conditionJson, 'Only top-level ALL or ANY condition groups can be edited visually.');
    }

    const rows = items.map((item, index) => toRow(item, fields, index));
    if (rows.some((row) => !row)) {
      return advanced(conditionJson, 'Nested or unsupported condition rows were detected.');
    }

    return toMappingResult(
      {
        mode,
        rows: rows as ConditionRow[],
        advancedJson: conditionJson,
        advancedReason: null
      },
      fields
    );
  } catch {
    return advanced(conditionJson, 'Condition JSON could not be parsed.');
  }
}

export function toConditionJson(
  state: ConditionBuilderState,
  fields: readonly AccessPolicyConditionField[]
): string | null {
  if (state.mode === 'none') {
    return null;
  }

  if (state.mode === 'advanced') {
    return state.advancedJson.trim() || null;
  }

  const rows = state.rows
    .map((row) => toJsonRow(row, fields))
    .filter((row): row is Record<string, unknown> => !!row);

  if (rows.length === 0) {
    return null;
  }

  return JSON.stringify({ [state.mode]: rows }, null, 2);
}

export function toMappingResult(
  state: ConditionBuilderState,
  fields: readonly AccessPolicyConditionField[]
): ConditionJsonMappingResult {
  const conditionJson = toConditionJson(state, fields);

  return {
    state,
    conditionJson,
    isSupported: state.mode !== 'advanced',
    isComplete: isComplete(state, fields)
  };
}

export function normalizeValueForField(
  value: unknown,
  field: AccessPolicyConditionField | undefined
): unknown {
  if (!field) {
    return value ?? '';
  }

  if (value === null || value === undefined) {
    return field.type === 'boolean' ? false : '';
  }

  if (field.type === 'boolean') {
    return value === true || value === 'true';
  }

  if (field.type === 'number') {
    return typeof value === 'number' ? value : Number(value);
  }

  if (field.type === 'date') {
    return value instanceof Date ? value : String(value);
  }

  return value;
}

export function newConditionRow(fields: readonly AccessPolicyConditionField[]): ConditionRow {
  const field = fields[0];

  return {
    id: crypto.randomUUID(),
    field: field?.field ?? '',
    operator: field?.operators[0] ?? 'eq',
    value: defaultValue(field),
    fieldType: field?.type
  };
}

function advanced(conditionJson: string, reason: string): ConditionJsonMappingResult {
  const state: ConditionBuilderState = {
    mode: 'advanced',
    rows: [],
    advancedJson: conditionJson,
    advancedReason: reason
  };

  return {
    state,
    conditionJson,
    isSupported: false,
    isComplete: conditionJson.trim().length > 0
  };
}

function toRow(
  item: unknown,
  fields: readonly AccessPolicyConditionField[],
  index: number
): ConditionRow | null {
  if (!item || typeof item !== 'object' || Array.isArray(item)) {
    return null;
  }

  const node = item as JsonCondition;
  if (typeof node.field !== 'string' || typeof node.operator !== 'string') {
    return null;
  }

  if ('all' in node || 'any' in node) {
    return null;
  }

  const field = fields.find((option) => option.field === node.field);
  if (!field || !field.operators.includes(node.operator)) {
    return null;
  }

  return {
    id: `existing-${index}-${node.field}`,
    field: node.field,
    operator: node.operator,
    value: normalizeValueForField(node.value, field),
    fieldType: field.type
  };
}

function toJsonRow(
  row: ConditionRow,
  fields: readonly AccessPolicyConditionField[]
): Record<string, unknown> | null {
  const field = fields.find((option) => option.field === row.field);
  if (!field || !row.operator || !field.operators.includes(row.operator)) {
    return null;
  }

  if (row.operator === 'isNull' || row.operator === 'notNull') {
    return {
      field: row.field,
      operator: row.operator
    };
  }

  return {
    field: row.field,
    operator: row.operator,
    value: serializeValue(row.value, field)
  };
}

function serializeValue(value: unknown, field: AccessPolicyConditionField): unknown {
  if (field.type === 'number') {
    return typeof value === 'number' ? value : Number(value);
  }

  if (field.type === 'boolean') {
    return value === true || value === 'true';
  }

  if (field.type === 'date') {
    return value instanceof Date ? value.toISOString() : value;
  }

  return value;
}

function isComplete(
  state: ConditionBuilderState,
  fields: readonly AccessPolicyConditionField[]
): boolean {
  if (state.mode === 'none') {
    return true;
  }

  if (state.mode === 'advanced') {
    return state.advancedJson.trim().length > 0;
  }

  return (
    state.rows.length > 0 &&
    state.rows.every((row) => {
      const field = fields.find((option) => option.field === row.field);
      return (
        !!field &&
        !!row.operator &&
        field.operators.includes(row.operator) &&
        (row.operator === 'isNull' || row.operator === 'notNull' || hasValue(row.value))
      );
    })
  );
}

function hasValue(value: unknown): boolean {
  return value !== null && value !== undefined && String(value).trim() !== '';
}

function defaultValue(field: AccessPolicyConditionField | undefined): unknown {
  if (!field) {
    return '';
  }

  if (field.type === 'boolean') {
    return true;
  }

  if (field.type === 'number') {
    return 0;
  }

  return field.options?.[0]?.value ?? '';
}

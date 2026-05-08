import { AccessPolicyConditionFieldType } from './access-policies.models';

export type ConditionMode = 'none' | 'all' | 'any' | 'advanced';

export interface ConditionRow {
  readonly id: string;
  readonly field: string;
  readonly operator: string;
  readonly value: unknown;
  readonly fieldType?: AccessPolicyConditionFieldType;
}

export interface ConditionBuilderState {
  readonly mode: ConditionMode;
  readonly rows: readonly ConditionRow[];
  readonly advancedJson: string;
  readonly advancedReason: string | null;
}

export interface ConditionJsonMappingResult {
  readonly state: ConditionBuilderState;
  readonly conditionJson: string | null;
  readonly isSupported: boolean;
  readonly isComplete: boolean;
}

export interface AccessPolicyAdminDto {
  readonly id: string;
  readonly resource: string;
  readonly action: string;
  readonly effect: string | number;
  readonly subjectType: string | number;
  readonly subjectKey: string;
  readonly conditionJson: string | null;
  readonly priority: number | null;
  readonly isEnabled: boolean;
  readonly description: string | null;
  readonly validFrom: string | null;
  readonly validTo: string | null;
  readonly deletedAt: string | null;
  readonly createdByUserId: string | null;
  readonly updatedByUserId: string | null;
}

export interface CreateAccessPolicyCommand {
  readonly resource: string;
  readonly action: string;
  readonly effect: string | number;
  readonly subjectType: string | number;
  readonly subjectKey: string;
  readonly conditionJson: string | null;
  readonly priority: number | null;
  readonly isEnabled: boolean;
  readonly description: string | null;
  readonly validFrom: string | null;
  readonly validTo: string | null;
}

export type UpdateAccessPolicyBody = CreateAccessPolicyCommand;

export interface AccessPolicyEnumOptionDto<TValue extends string | number = string | number> {
  readonly value: TValue;
  readonly label: string;
}

export interface AccessPolicyOptionsDto {
  readonly resources: readonly string[];
  readonly actionsByResource: Record<string, readonly string[]>;
  readonly effects: readonly AccessPolicyEnumOptionDto[];
  readonly subjectTypes: readonly AccessPolicyEnumOptionDto[];
  readonly conditionFieldsByResource: Record<string, readonly AccessPolicyConditionField[]>;
  readonly effectOptions?: readonly AccessPolicyEnumOptionDto[];
  readonly subjectTypeOptions?: readonly AccessPolicyEnumOptionDto[];
}

export type AccessPolicyConditionFieldType =
  | 'string'
  | 'number'
  | 'boolean'
  | 'date'
  | 'guid'
  | 'enum';

export interface AccessPolicyConditionOperator {
  readonly value: string;
  readonly label: string;
}

export interface AccessPolicyConditionEnumOption {
  readonly value: string | number | boolean;
  readonly label: string;
}

export interface AccessPolicyConditionField {
  readonly field: string;
  readonly label: string;
  readonly type: AccessPolicyConditionFieldType;
  readonly operators: readonly string[];
  readonly options?: readonly AccessPolicyConditionEnumOption[] | null;
}

export interface ValidateAccessPolicyConditionCommand {
  readonly resource: string;
  readonly conditionJson: string;
}

export interface ValidateAccessPolicyConditionResultDto {
  readonly isValid: boolean;
  readonly errorMessage: string | null;
}

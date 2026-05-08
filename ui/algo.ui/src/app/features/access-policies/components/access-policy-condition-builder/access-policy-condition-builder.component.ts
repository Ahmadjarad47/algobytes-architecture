import {
  ChangeDetectionStrategy,
  Component,
  OnChanges,
  SimpleChanges,
  computed,
  input,
  output,
  signal
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { DatePickerModule } from 'primeng/datepicker';
import { InputNumberModule } from 'primeng/inputnumber';
import { InputTextModule } from 'primeng/inputtext';
import { MessageModule } from 'primeng/message';
import { PanelModule } from 'primeng/panel';
import { SelectModule } from 'primeng/select';
import { TextareaModule } from 'primeng/textarea';

import { AccessPolicyConditionField } from '../../models/access-policies.models';
import {
  ConditionBuilderState,
  ConditionMode,
  ConditionRow
} from '../../models/condition-builder.models';
import {
  createEmptyConditionState,
  newConditionRow,
  normalizeValueForField,
  parseConditionJson,
  toConditionJson,
  toMappingResult
} from '../../models/condition-json.mapper';

@Component({
  selector: 'app-access-policy-condition-builder',
  imports: [
    FormsModule,
    ButtonModule,
    DatePickerModule,
    InputNumberModule,
    InputTextModule,
    MessageModule,
    PanelModule,
    SelectModule,
    TextareaModule
  ],
  templateUrl: './access-policy-condition-builder.component.html',
  styleUrl: './access-policy-condition-builder.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AccessPolicyConditionBuilderComponent implements OnChanges {
  readonly resource = input.required<string>();
  readonly conditionJson = input<string | null>(null);
  readonly fields = input<readonly AccessPolicyConditionField[]>([]);
  readonly validationMessage = input<string | null>(null);
  readonly validationSeverity = input<'success' | 'error' | 'info' | 'warn'>('info');

  readonly conditionJsonChange = output<string | null>();
  readonly completeChange = output<boolean>();
  readonly advancedChange = output<boolean>();
  readonly validateRequested = output<void>();

  protected readonly state = signal<ConditionBuilderState>(createEmptyConditionState());

  protected readonly modeOptions = [
    { label: 'No condition', value: 'none' },
    { label: 'ALL conditions', value: 'all' },
    { label: 'ANY conditions', value: 'any' }
  ];

  protected readonly booleanOptions = [
    { label: 'True', value: true },
    { label: 'False', value: false }
  ];

  protected readonly fieldOptions = computed(() =>
    this.fields().map((field) => ({
      label: field.label,
      value: field.field
    }))
  );

  protected readonly previewJson = computed(() => toConditionJson(this.state(), this.fields()) ?? '');

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['conditionJson'] || changes['fields'] || changes['resource']) {
      const parsed = parseConditionJson(this.conditionJson(), this.fields());
      this.state.set(parsed.state);
      this.emitState();
    }
  }

  protected setMode(mode: ConditionMode): void {
    if (mode === 'advanced') {
      return;
    }

    this.state.update((state) => ({
      ...state,
      mode,
      rows: mode === 'none' ? [] : state.rows.length ? state.rows : [newConditionRow(this.fields())],
      advancedReason: null
    }));
    this.emitState();
  }

  protected addRow(): void {
    this.state.update((state) => ({
      ...state,
      mode: state.mode === 'none' ? 'all' : state.mode,
      rows: [...state.rows, newConditionRow(this.fields())]
    }));
    this.emitState();
  }

  protected removeRow(id: string): void {
    this.state.update((state) => ({
      ...state,
      rows: state.rows.filter((row) => row.id !== id)
    }));
    this.emitState();
  }

  protected updateField(row: ConditionRow, fieldKey: string): void {
    const field = this.findField(fieldKey);
    this.updateRow(row.id, {
      field: fieldKey,
      operator: field?.operators[0] ?? 'eq',
      value: normalizeValueForField(null, field),
      fieldType: field?.type
    });
  }

  protected updateOperator(row: ConditionRow, operator: string): void {
    this.updateRow(row.id, { operator });
  }

  protected updateValue(row: ConditionRow, value: unknown): void {
    this.updateRow(row.id, { value });
  }

  protected enableAdvancedEdit(): void {
    this.state.update((state) => ({
      ...state,
      advancedJson: state.advancedJson || this.previewJson(),
      mode: 'advanced'
    }));
    this.emitState();
  }

  protected updateAdvancedJson(value: string): void {
    this.state.update((state) => ({
      ...state,
      advancedJson: value
    }));
    this.emitState();
  }

  protected fieldFor(row: ConditionRow): AccessPolicyConditionField | undefined {
    return this.findField(row.field);
  }

  protected enumOptions(field: AccessPolicyConditionField | undefined): { label: string; value: unknown }[] {
    return [...(field?.options ?? [])];
  }

  protected operatorOptions(row: ConditionRow): { label: string; value: string }[] {
    return (
      this.fieldFor(row)?.operators.map((operator) => ({
        label: this.operatorLabel(operator),
        value: operator
      })) ?? []
    );
  }

  private updateRow(id: string, patch: Partial<ConditionRow>): void {
    this.state.update((state) => ({
      ...state,
      rows: state.rows.map((row) => (row.id === id ? { ...row, ...patch } : row))
    }));
    this.emitState();
  }

  private emitState(): void {
    const result = toMappingResult(this.state(), this.fields());
    this.conditionJsonChange.emit(result.conditionJson);
    this.completeChange.emit(result.isComplete);
    this.advancedChange.emit(!result.isSupported);
  }

  private findField(fieldKey: string): AccessPolicyConditionField | undefined {
    return this.fields().find((field) => field.field === fieldKey);
  }

  private operatorLabel(operator: string): string {
    const labels: Record<string, string> = {
      eq: 'Equals',
      neq: 'Does not equal',
      gt: 'Greater than',
      gte: 'Greater than or equal',
      lt: 'Less than',
      lte: 'Less than or equal',
      in: 'In',
      nin: 'Not in',
      contains: 'Contains',
      startsWith: 'Starts with',
      endsWith: 'Ends with',
      isNull: 'Is null',
      notNull: 'Is not null'
    };

    return labels[operator] ?? operator;
  }
}

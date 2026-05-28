import { describe, expect, it } from 'vitest';

import { CustomFieldDefinition } from '../models/custom-fields.models';
import { customFieldFormFields } from './custom-field.utils';

describe('customFieldFormFields', () => {
  it('maps object options by label and value', () => {
    const fields = customFieldFormFields([
      definition({
        options: [
          { label: 'Male', value: 'male' },
          { label: 'Female', value: 'female' }
        ]
      })
    ]);

    expect(fields[0].options).toEqual([
      { label: 'Male', value: 'male' },
      { label: 'Female', value: 'female' }
    ]);
  });

  it('keeps string options supported', () => {
    const fields = customFieldFormFields([definition({ options: ['Admin', 'User'] })]);

    expect(fields[0].options).toEqual([
      { label: 'Admin', value: 'Admin' },
      { label: 'User', value: 'User' }
    ]);
  });
});

function definition(overrides: Partial<CustomFieldDefinition> = {}): CustomFieldDefinition {
  return {
    id: 'field-id',
    entity: 'users',
    key: 'gender',
    label: 'Gender',
    type: 'select',
    required: false,
    searchable: false,
    filterable: false,
    sortable: false,
    visibleInTable: true,
    visibleInForm: true,
    visibleInDetails: true,
    options: null,
    defaultValue: null,
    validation: null,
    createdAt: '2026-05-27T00:00:00.000Z',
    updatedAt: '2026-05-27T00:00:00.000Z',
    ...overrides
  };
}

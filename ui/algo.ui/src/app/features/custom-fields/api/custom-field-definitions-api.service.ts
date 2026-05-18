import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiService } from '../../../core/api/api.service';
import {
  CreateCustomFieldDefinitionCommand,
  CustomFieldDefinition,
  CustomFieldEntity,
  UpdateCustomFieldDefinitionBody
} from '../models/custom-fields.models';

@Injectable({ providedIn: 'root' })
export class CustomFieldDefinitionsApiService {
  private readonly api = inject(ApiService);

  getDefinitions(entity: CustomFieldEntity): Observable<CustomFieldDefinition[]> {
    return this.api.get<CustomFieldDefinition[]>('/custom-field-definitions', { entity });
  }

  createDefinition(command: CreateCustomFieldDefinitionCommand): Observable<CustomFieldDefinition> {
    return this.api.post<CustomFieldDefinition, CreateCustomFieldDefinitionCommand>('/custom-field-definitions', command);
  }

  updateDefinition(id: string, body: UpdateCustomFieldDefinitionBody): Observable<CustomFieldDefinition> {
    return this.api.put<CustomFieldDefinition, UpdateCustomFieldDefinitionBody>(`/custom-field-definitions/${id}`, body);
  }

  deleteDefinition(id: string): Observable<void> {
    return this.api.delete<void>(`/custom-field-definitions/${id}`);
  }
}

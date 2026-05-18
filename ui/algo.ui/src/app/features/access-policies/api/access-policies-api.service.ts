import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiService } from '../../../core/api/api.service';
import {
  AccessPolicyAdminDto,
  AccessPolicyOptionsDto,
  CreateAccessPolicyCommand,
  UpdateAccessPolicyBody,
  ValidateAccessPolicyConditionCommand,
  ValidateAccessPolicyConditionResultDto
} from '../models/access-policies.models';

@Injectable({ providedIn: 'root' })
export class AccessPoliciesApiService {
  private readonly api = inject(ApiService);

  getPolicies(query?: { includeTrashed?: boolean; onlyTrashed?: boolean }): Observable<AccessPolicyAdminDto[]> {
    return this.api.get<AccessPolicyAdminDto[]>('/AccessPolicies', query);
  }

  getPolicy(id: string): Observable<AccessPolicyAdminDto> {
    return this.api.get<AccessPolicyAdminDto>(`/AccessPolicies/${id}`);
  }

  getOptions(): Observable<AccessPolicyOptionsDto> {
    return this.api.get<AccessPolicyOptionsDto>('/AccessPolicies/options');
  }

  createPolicy(
    command: CreateAccessPolicyCommand
  ): Observable<AccessPolicyAdminDto> {
    return this.api.post<AccessPolicyAdminDto, CreateAccessPolicyCommand>(
      '/AccessPolicies',
      command
    );
  }

  updatePolicy(
    id: string,
    command: UpdateAccessPolicyBody
  ): Observable<AccessPolicyAdminDto> {
    return this.api.put<AccessPolicyAdminDto, UpdateAccessPolicyBody>(
      `/AccessPolicies/${id}`,
      command
    );
  }

  setEnabled(id: string, isEnabled: boolean): Observable<void> {
    return this.api.patch<void, { isEnabled: boolean }>(
      `/AccessPolicies/${id}/enabled`,
      { isEnabled }
    );
  }

  deletePolicy(id: string): Observable<void> {
    return this.api.delete<void>(`/AccessPolicies/${id}`);
  }

  restorePolicy(id: string): Observable<void> {
    return this.api.patch<void>(`/AccessPolicies/${id}/restore`);
  }

  validateCondition(
    command: ValidateAccessPolicyConditionCommand
  ): Observable<ValidateAccessPolicyConditionResultDto> {
    return this.api.post<
      ValidateAccessPolicyConditionResultDto,
      ValidateAccessPolicyConditionCommand
    >('/AccessPolicies/validate-condition', command);
  }
}

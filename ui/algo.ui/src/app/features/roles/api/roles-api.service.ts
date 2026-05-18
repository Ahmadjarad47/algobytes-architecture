import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiService } from '../../../core/api/api.service';
import {
  CreateRoleCommand,
  RoleDetailsDto,
  RoleDto,
  UpdateRoleRequest
} from '../models/roles.models';

@Injectable({ providedIn: 'root' })
export class RolesApiService {
  private readonly api = inject(ApiService);

  getRoles(query?: { includeTrashed?: boolean; onlyTrashed?: boolean }): Observable<RoleDto[]> {
    return this.api.get<RoleDto[]>('/Roles', query);
  }

  getRole(id: string): Observable<RoleDetailsDto> {
    return this.api.get<RoleDetailsDto>(`/Roles/${id}`);
  }

  createRole(command: CreateRoleCommand): Observable<RoleDetailsDto> {
    return this.api.post<RoleDetailsDto, CreateRoleCommand>('/Roles', command);
  }

  updateRole(id: string, command: UpdateRoleRequest): Observable<RoleDetailsDto> {
    return this.api.put<RoleDetailsDto, UpdateRoleRequest>(`/Roles/${id}`, command);
  }

  deleteRole(id: string): Observable<void> {
    return this.api.delete<void>(`/Roles/${id}`);
  }

  restoreRole(id: string): Observable<void> {
    return this.api.patch<void>(`/Roles/${id}/restore`);
  }
}

import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiService } from '../../../core/api/api.service';
import {
  AssignRolesRequest,
  CreateUserCommand,
  UpdateUserRequest,
  UserDetails,
  UsersPage,
  UsersQuery
} from '../models/users.models';

@Injectable({ providedIn: 'root' })
export class UsersApiService {
  private readonly api = inject(ApiService);

  getUsers(query: UsersQuery): Observable<UsersPage> {
    return this.api.get<UsersPage>('/Users', query);
  }

  getUser(id: string): Observable<UserDetails> {
    return this.api.get<UserDetails>(`/Users/${id}`);
  }

  createUser(command: CreateUserCommand): Observable<UserDetails> {
    return this.api.post<UserDetails, CreateUserCommand>('/Users', command);
  }

  updateUser(id: string, command: UpdateUserRequest): Observable<UserDetails> {
    return this.api.put<UserDetails, UpdateUserRequest>(`/Users/${id}`, command);
  }

  activateUser(id: string): Observable<void> {
    return this.api.patch<void>(`/Users/${id}/activate`);
  }

  deactivateUser(id: string): Observable<void> {
    return this.api.patch<void>(`/Users/${id}/deactivate`);
  }

  lockUser(id: string): Observable<void> {
    const lockoutEnd = new Date();
    lockoutEnd.setFullYear(lockoutEnd.getFullYear() + 1);

    return this.api.patch<void, { lockoutEnd: string }>(`/Users/${id}/lock`, {
      lockoutEnd: lockoutEnd.toISOString()
    });
  }

  unlockUser(id: string): Observable<void> {
    return this.api.patch<void>(`/Users/${id}/unlock`);
  }

  deleteUser(id: string): Observable<void> {
    return this.api.delete<void>(`/Users/${id}`);
  }

  assignRoles(id: string, roles: readonly string[]): Observable<void> {
    return this.api.post<void, AssignRolesRequest>(`/Users/${id}/roles`, { roles });
  }
}

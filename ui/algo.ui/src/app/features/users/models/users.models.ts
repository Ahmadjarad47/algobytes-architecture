import { PaginatedResult } from '../../../core/models/paginated-result.model';

export interface UserListItem {
  readonly id: string;
  readonly email: string | null;
  readonly userName: string | null;
  readonly displayName: string;
  readonly phoneNumber: string | null;
  readonly isActive: boolean;
  readonly isLocked: boolean;
  readonly emailConfirmed: boolean;
  readonly phoneNumberConfirmed: boolean;
  readonly createdAt: string;
  readonly updatedAt: string;
  readonly lastLoginAt: string | null;
  readonly isOnline: boolean;
  readonly twoFactorEnabled: boolean;
  readonly totpRequiredByAdmin: boolean;
  readonly roles: readonly string[];
}

export interface UserDetails {
  readonly userId: string;
  readonly email: string | null;
  readonly userName: string | null;
  readonly displayName: string;
  readonly phoneNumber: string | null;
  readonly emailConfirmed: boolean;
  readonly phoneNumberConfirmed: boolean;
  readonly isActive: boolean;
  readonly isLocked: boolean;
  readonly lockoutEnd: string | null;
  readonly createdAt: string;
  readonly updatedAt: string;
  readonly lastLoginAt: string | null;
  readonly twoFactorEnabled: boolean;
  readonly totpRequiredByAdmin: boolean;
  readonly roles: readonly string[];
}

export interface UserPermissionGraphNode {
  readonly id: string;
  readonly type: 'user' | 'role' | 'policy' | 'resource' | string;
  readonly label: string;
  readonly resource?: string | null;
  readonly action?: string | null;
  readonly effect?: string | null;
  readonly conditionJson?: string | null;
  readonly priority?: number | null;
  readonly isEnabled?: boolean | null;
}

export interface UserPermissionGraphEdge {
  readonly from: string;
  readonly to: string;
  readonly type: 'hasRole' | 'hasPolicy' | 'grants' | string;
}

export interface UserPermissionGraph {
  readonly userId: string;
  readonly nodes: readonly UserPermissionGraphNode[];
  readonly edges: readonly UserPermissionGraphEdge[];
}

export interface UsersQuery {
  readonly PageNumber: number;
  readonly PageSize: number;
  readonly Search?: string;
  readonly SortField?: string;
  readonly SortDirection?: 'Ascending' | 'Descending';
  readonly IsActive?: boolean;
  readonly IsLocked?: boolean;
  readonly EmailConfirmed?: boolean;
  readonly PhoneNumberConfirmed?: boolean;
}

export interface CreateUserCommand {
  readonly email: string;
  readonly userName: string;
  readonly displayName: string;
  readonly phoneNumber: string | null;
  readonly password: string;
  readonly confirmPassword: string;
  readonly roles: readonly string[];
  readonly emailConfirmed: boolean;
  readonly isActive: boolean;
}

export interface UpdateUserRequest {
  readonly displayName?: string | null;
  readonly phoneNumber?: string | null;
  readonly userName?: string | null;
  readonly isActive?: boolean | null;
  readonly emailConfirmed?: boolean | null;
}

export interface AssignRolesRequest {
  readonly roles: readonly string[];
}

export interface SetUserTotpPolicyRequest {
  readonly isRequired: boolean;
}

export type UsersPage = PaginatedResult<UserListItem>;

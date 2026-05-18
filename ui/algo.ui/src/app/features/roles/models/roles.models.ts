export interface RoleDto {
  readonly id: string;
  readonly name: string;
  readonly normalizedName: string | null;
  readonly trashedAt: string | null;
  readonly trashExpiresAt: string | null;
  readonly deletedAt: string | null;
  readonly customFields: Record<string, unknown> | null;
}

export interface RoleDetailsDto extends RoleDto {
  readonly userCount: number | null;
}

export interface CreateRoleCommand {
  readonly name: string;
  readonly customFields?: Record<string, unknown> | null;
}

export interface UpdateRoleRequest {
  readonly name: string;
  readonly customFields?: Record<string, unknown> | null;
}

export interface RoleDto {
  readonly id: string;
  readonly name: string;
  readonly normalizedName: string | null;
}

export interface RoleDetailsDto extends RoleDto {
  readonly userCount: number | null;
}

export interface CreateRoleCommand {
  readonly name: string;
}

export interface UpdateRoleRequest {
  readonly name: string;
}

export type PermissionAction = 'read' | 'create' | 'update' | 'delete' | 'revoke' | 'export';

export interface AppPermission {
  readonly resource: string;
  readonly action: PermissionAction | '*';
}

export interface PermissionGate {
  readonly all?: readonly AppPermission[];
  readonly any?: readonly AppPermission[];
}

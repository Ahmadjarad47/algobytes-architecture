import { AppPermission } from './permission.types';

export const Permissions = {
  users: {
    read: { resource: 'users', action: 'read' } satisfies AppPermission,
    create: { resource: 'users', action: 'create' } satisfies AppPermission,
    update: { resource: 'users', action: 'update' } satisfies AppPermission,
    delete: { resource: 'users', action: 'delete' } satisfies AppPermission
  },
  roles: {
    read: { resource: 'roles', action: 'read' } satisfies AppPermission,
    create: { resource: 'roles', action: 'create' } satisfies AppPermission,
    update: { resource: 'roles', action: 'update' } satisfies AppPermission,
    delete: { resource: 'roles', action: 'delete' } satisfies AppPermission
  },
  accessPolicies: {
    read: { resource: 'accessPolicies', action: 'read' } satisfies AppPermission,
    create: { resource: 'accessPolicies', action: 'create' } satisfies AppPermission,
    update: { resource: 'accessPolicies', action: 'update' } satisfies AppPermission,
    delete: { resource: 'accessPolicies', action: 'delete' } satisfies AppPermission
  },
  sessions: {
    read: { resource: 'sessions', action: 'read' } satisfies AppPermission,
    revoke: { resource: 'sessions', action: 'revoke' } satisfies AppPermission,
    revokeAll: { resource: 'sessions', action: 'revoke' } satisfies AppPermission,
    export: { resource: 'sessions', action: 'export' } satisfies AppPermission
  },
  logs: {
    read: { resource: 'logs', action: 'read' } satisfies AppPermission,
    export: { resource: 'logs', action: 'export' } satisfies AppPermission
  },
  errorLogs: {
    read: { resource: 'errorLogs', action: 'read' } satisfies AppPermission,
    export: { resource: 'errorLogs', action: 'export' } satisfies AppPermission
  },
  products: {
    read: { resource: 'products', action: 'read' } satisfies AppPermission,
    create: { resource: 'products', action: 'create' } satisfies AppPermission,
    update: { resource: 'products', action: 'update' } satisfies AppPermission,
    delete: { resource: 'products', action: 'delete' } satisfies AppPermission
  },
  categories: {
    read: { resource: 'categories', action: 'read' } satisfies AppPermission,
    create: { resource: 'categories', action: 'create' } satisfies AppPermission,
    update: { resource: 'categories', action: 'update' } satisfies AppPermission,
    delete: { resource: 'categories', action: 'delete' } satisfies AppPermission
  },
  settings: {
    read: { resource: 'settings', action: 'read' } satisfies AppPermission,
    update: { resource: 'settings', action: 'update' } satisfies AppPermission
  },
  reports: {
    read: { resource: 'reports', action: 'read' } satisfies AppPermission
  }
} as const;

export const DashboardOverviewReadPermissions: readonly AppPermission[] = [
  Permissions.roles.read,
  Permissions.accessPolicies.read,
  Permissions.sessions.read,
  Permissions.logs.read,
  Permissions.errorLogs.read,
  Permissions.reports.read
];

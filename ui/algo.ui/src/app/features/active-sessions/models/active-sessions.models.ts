export type SessionStatus = 'Online' | 'Idle' | 'Offline' | 'Expired' | 'Revoked';
export type SessionDeviceType = 'Desktop' | 'Laptop' | 'Tablet' | 'Mobile';

export interface ActiveSession {
  readonly id: string;
  readonly userId: string;
  readonly userName: string;
  readonly email: string;
  readonly role: string;
  readonly status: SessionStatus;
  readonly device: SessionDeviceType;
  readonly browser: string;
  readonly os: string;
  readonly ipAddress: string;
  readonly location: string;
  readonly loginTime: string;
  readonly lastActivity: string;
  readonly durationMinutes: number;
  readonly expiresAt: string;
  readonly refreshTokenExpiresAt: string | null;
  readonly currentAdminSession: boolean;
  readonly trustedDevice: boolean;
  readonly suspicious: boolean;
  readonly revokedAt: string | null;
  readonly revokedBy: string | null;
  readonly userAgent: string;
  readonly activityTimeline: readonly string[];
}

export interface ActiveSessionsQuery {
  readonly search?: string;
  readonly status?: SessionStatus | 'All';
  readonly role?: string;
  readonly device?: SessionDeviceType | 'All';
  readonly browser?: string;
  readonly from?: string;
  readonly to?: string;
  readonly suspiciousOnly?: boolean;
}

export interface ActiveSessionsSummary {
  readonly onlineUsers: number;
  readonly idleUsers: number;
  readonly activeSessions: number;
  readonly suspiciousSessions: number;
  readonly revokedToday: number;
}

export interface SessionAuditEvent {
  readonly actor: string;
  readonly action: 'session.revoked' | 'session.userSessionsRevoked' | 'session.bulkRevoked';
  readonly targetUser: string;
  readonly targetSession: string | null;
  readonly timestamp: string;
  readonly ipAddress: string;
}

export interface ActiveSessionPermissions {
  readonly view: boolean;
  readonly revoke: boolean;
  readonly revokeAll: boolean;
  readonly export: boolean;
}

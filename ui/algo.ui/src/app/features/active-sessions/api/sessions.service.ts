import { inject, Injectable, signal } from '@angular/core';
import { Observable, of, throwError } from 'rxjs';
import { catchError, delay, map } from 'rxjs/operators';

import { ApiService } from '../../../core/api/api.service';
import { Permissions } from '../../../core/permissions/permission.catalog';
import { PermissionService } from '../../../core/permissions/permission.service';
import { PaginatedResult } from '../../../core/models/paginated-result.model';
import {
  ActiveSession,
  ActiveSessionPermissions,
  ActiveSessionsQuery,
  ActiveSessionsSummary,
  SessionAuditEvent
} from '../models/active-sessions.models';

const NOW = new Date();

interface ActiveSessionsApiResponse {
  readonly sessions: PaginatedResult<ActiveSession>;
  readonly summary: ActiveSessionsSummary;
}

interface RevokeCountResponse {
  readonly count: number;
}

@Injectable({ providedIn: 'root' })
export class SessionsService {
  private readonly api = inject(ApiService);
  private readonly permissionService = inject(PermissionService);
  private readonly sessionsState = signal<ActiveSession[]>(createMockSessions());
  private readonly auditEventsState = signal<SessionAuditEvent[]>([]);
  private readonly latestSummaryState = signal<ActiveSessionsSummary | null>(null);

  get permissions(): ActiveSessionPermissions {
    return {
      view: this.permissionService.can({ any: [Permissions.sessions.read] }),
      revoke: this.permissionService.can({ any: [Permissions.sessions.revoke] }),
      revokeAll: this.permissionService.can({ any: [Permissions.sessions.revokeAll] }),
      export: this.permissionService.can({ any: [Permissions.sessions.read] })
    };
  }

  getSessions(params: ActiveSessionsQuery = {}): Observable<ActiveSession[]> {
    return this.api.get<ActiveSessionsApiResponse>('/Sessions', toApiQuery(params)).pipe(
      map((response) => {
        this.latestSummaryState.set(response.summary);
        return response.sessions.items;
      }),
      catchError(() => this.getMockSessions(params))
    );
  }

  getSession(id: string): Observable<ActiveSession> {
    return this.api.get<ActiveSession>(`/Sessions/${id}`).pipe(
      catchError(() => this.getMockSession(id))
    );
  }

  getSummary(params: ActiveSessionsQuery = {}): Observable<ActiveSessionsSummary> {
    return this.api.get<ActiveSessionsApiResponse>('/Sessions', toApiQuery(params)).pipe(
      map((response) => {
        this.latestSummaryState.set(response.summary);
        return response.summary;
      }),
      catchError(() => this.getMockSessions(params).pipe(map(toSummary)))
    );
  }

  revokeSession(id: string, actor = 'Current admin', confirmCurrentSession = false): Observable<void> {
    return this.api
      .post<void, { confirmCurrentSession: boolean }>(`/Sessions/${id}/revoke`, { confirmCurrentSession })
      .pipe(catchError(() => this.revokeMockSession(id, actor)));
  }

  revokeUserSessions(userId: string, actor = 'Current admin', confirmCurrentUser = false): Observable<void> {
    return this.api
      .post<RevokeCountResponse, { confirmCurrentUser: boolean }>(`/Sessions/users/${userId}/revoke`, { confirmCurrentUser })
      .pipe(
        map(() => undefined),
        catchError(() => this.revokeMockUserSessions(userId, actor))
      );
  }

  revokeSelectedSessions(ids: readonly string[], actor = 'Current admin'): Observable<void> {
    return this.api
      .post<RevokeCountResponse, { ids: readonly string[] }>('/Sessions/revoke-selected', { ids })
      .pipe(
        map(() => undefined),
        catchError(() => this.revokeMockSelectedSessions(ids, actor))
      );
  }

  revokeAllExceptCurrent(actor = 'Current admin', confirmation = 'LOGOUT'): Observable<void> {
    return this.api
      .post<RevokeCountResponse, { confirmation: string }>('/Sessions/revoke-all-except-current', { confirmation })
      .pipe(
        map(() => undefined),
        catchError(() => this.revokeMockAllExceptCurrent(actor))
      );
  }

  auditEvents(): readonly SessionAuditEvent[] {
    return this.auditEventsState();
  }

  private getMockSessions(params: ActiveSessionsQuery = {}): Observable<ActiveSession[]> {
    return of(this.sessionsState()).pipe(
      delay(150),
      map((sessions) => filterSessions(sessions, params))
    );
  }

  private getMockSession(id: string): Observable<ActiveSession> {
    const session = this.sessionsState().find((item) => item.id === id);

    return session
      ? of(session).pipe(delay(120))
      : throwError(() => new Error('Session was not found.'));
  }

  private revokeMockSession(id: string, actor = 'Current admin'): Observable<void> {
    const session = this.sessionsState().find((item) => item.id === id);
    if (!session) {
      return throwError(() => new Error('Session was not found.'));
    }

    this.revokeWhere((item) => item.id === id, actor, 'session.revoked', session.userName, session.id);
    return of(undefined).pipe(delay(180));
  }

  private revokeMockUserSessions(userId: string, actor = 'Current admin'): Observable<void> {
    const session = this.sessionsState().find((item) => item.userId === userId);
    if (!session) {
      return throwError(() => new Error('User sessions were not found.'));
    }

    this.revokeWhere(
      (item) => item.userId === userId,
      actor,
      'session.userSessionsRevoked',
      session.userName,
      null
    );
    return of(undefined).pipe(delay(220));
  }

  private revokeMockSelectedSessions(ids: readonly string[], actor = 'Current admin'): Observable<void> {
    const idSet = new Set(ids);
    this.revokeWhere(
      (item) => idSet.has(item.id),
      actor,
      'session.bulkRevoked',
      `${ids.length} selected sessions`,
      null
    );
    return of(undefined).pipe(delay(220));
  }

  private revokeMockAllExceptCurrent(actor = 'Current admin'): Observable<void> {
    this.revokeWhere(
      (item) => !item.currentAdminSession,
      actor,
      'session.bulkRevoked',
      'All users except current admin',
      null
    );
    return of(undefined).pipe(delay(260));
  }

  private revokeWhere(
    predicate: (session: ActiveSession) => boolean,
    actor: string,
    action: SessionAuditEvent['action'],
    targetUser: string,
    targetSession: string | null
  ): void {
    const timestamp = new Date().toISOString();

    this.sessionsState.update((sessions) =>
      sessions.map((session) =>
        predicate(session)
          ? {
              ...session,
              status: 'Revoked',
              revokedAt: timestamp,
              revokedBy: actor
            }
          : session
      )
    );

    this.auditEventsState.update((events) => [
      {
        actor,
        action,
        targetUser,
        targetSession,
        timestamp,
        ipAddress: '127.0.0.1'
      },
      ...events
    ]);
  }
}

function filterSessions(sessions: readonly ActiveSession[], params: ActiveSessionsQuery): ActiveSession[] {
  const search = params.search?.trim().toLowerCase();
  const from = params.from ? new Date(params.from).getTime() : null;
  const to = params.to ? new Date(params.to).getTime() : null;

  return sessions.filter((session) => {
    const loginTime = new Date(session.loginTime).getTime();

    return (
      (!search ||
        session.userName.toLowerCase().includes(search) ||
        session.email.toLowerCase().includes(search) ||
        session.ipAddress.toLowerCase().includes(search)) &&
      (!params.status || params.status === 'All' || session.status === params.status) &&
      (!params.role || params.role === 'All' || session.role === params.role) &&
      (!params.device || params.device === 'All' || session.device === params.device) &&
      (!params.browser || params.browser === 'All' || session.browser === params.browser) &&
      (!params.suspiciousOnly || session.suspicious) &&
      (from === null || loginTime >= from) &&
      (to === null || loginTime <= to)
    );
  });
}

function toSummary(sessions: readonly ActiveSession[]): ActiveSessionsSummary {
  const today = new Date().toDateString();

  return {
    onlineUsers: new Set(sessions.filter((session) => session.status === 'Online').map((session) => session.userId)).size,
    idleUsers: new Set(sessions.filter((session) => session.status === 'Idle').map((session) => session.userId)).size,
    activeSessions: sessions.filter((session) => session.status === 'Online' || session.status === 'Idle').length,
    suspiciousSessions: sessions.filter((session) => session.suspicious).length,
    revokedToday: sessions.filter((session) => session.revokedAt && new Date(session.revokedAt).toDateString() === today).length
  };
}

function toApiQuery(params: ActiveSessionsQuery): Record<string, string | number | boolean | undefined> {
  return {
    PageNumber: 1,
    PageSize: 100,
    Search: params.search,
    Status: params.status && params.status !== 'All' ? params.status : undefined,
    Role: params.role && params.role !== 'All' ? params.role : undefined,
    Device: params.device && params.device !== 'All' ? params.device : undefined,
    Browser: params.browser && params.browser !== 'All' ? params.browser : undefined,
    From: params.from,
    To: params.to,
    SuspiciousOnly: params.suspiciousOnly
  };
}

function createMockSessions(): ActiveSession[] {
  return [
    mockSession('sess-current', 'usr-admin', 'Super Admin', 'admin@algo.bytes', 'Admin', 'Online', 'Laptop', 'Chrome', 'Windows 11', '10.0.0.18', 'Riyadh, SA', -38, 480, true, false),
    mockSession('sess-amelia-1', 'usr-amelia', 'Amelia Hart', 'amelia@example.com', 'Admin', 'Online', 'Desktop', 'Edge', 'Windows 11', '203.0.113.12', 'London, UK', -74, 360, false, false),
    mockSession('sess-amelia-2', 'usr-amelia', 'Amelia Hart', 'amelia@example.com', 'Admin', 'Idle', 'Mobile', 'Safari', 'iOS 18', '198.51.100.44', 'Manchester, UK', -410, 120, false, false),
    mockSession('sess-omar-1', 'usr-omar', 'Omar Saleh', 'omar@example.com', 'Operations', 'Idle', 'Laptop', 'Firefox', 'macOS', '192.0.2.88', 'Dubai, AE', -190, 240, false, true),
    mockSession('sess-nora-1', 'usr-nora', 'Nora Kim', 'nora@example.com', 'Support', 'Offline', 'Tablet', 'Chrome', 'Android', '172.16.4.9', 'Seoul, KR', -980, 60, false, false),
    mockSession('sess-jules-1', 'usr-jules', 'Jules Martin', 'jules@example.com', 'Viewer', 'Expired', 'Desktop', 'Chrome', 'Ubuntu', '203.0.113.77', 'Paris, FR', -1440, -10, false, false),
    {
      ...mockSession('sess-suspicious-1', 'usr-maya', 'Maya Chen', 'maya@example.com', 'Finance', 'Online', 'Laptop', 'Unknown', 'Linux', '45.83.12.91', 'Unknown location', -22, 90, false, true),
      userAgent: 'Unknown browser from unusual ASN'
    },
    {
      ...mockSession('sess-revoked-1', 'usr-lee', 'Lee Carter', 'lee@example.com', 'Support', 'Revoked', 'Mobile', 'Chrome', 'Android', '198.51.100.22', 'Austin, US', -320, 30, false, true),
      revokedAt: new Date(NOW.getTime() - 35 * 60_000).toISOString(),
      revokedBy: 'Security Admin'
    }
  ];
}

function mockSession(
  id: string,
  userId: string,
  userName: string,
  email: string,
  role: string,
  status: ActiveSession['status'],
  device: ActiveSession['device'],
  browser: string,
  os: string,
  ipAddress: string,
  location: string,
  loginOffsetMinutes: number,
  expiresInMinutes: number,
  currentAdminSession: boolean,
  suspicious: boolean
): ActiveSession {
  const login = new Date(NOW.getTime() + loginOffsetMinutes * 60_000);
  const lastActivity = new Date(NOW.getTime() - Math.min(Math.abs(loginOffsetMinutes), 18) * 60_000);
  const expiresAt = new Date(NOW.getTime() + expiresInMinutes * 60_000);

  return {
    id,
    userId,
    userName,
    email,
    role,
    status,
    device,
    browser,
    os,
    ipAddress,
    location,
    loginTime: login.toISOString(),
    lastActivity: lastActivity.toISOString(),
    durationMinutes: Math.max(1, Math.round((NOW.getTime() - login.getTime()) / 60_000)),
    expiresAt: expiresAt.toISOString(),
    refreshTokenExpiresAt: new Date(expiresAt.getTime() + 7 * 24 * 60 * 60_000).toISOString(),
    currentAdminSession,
    trustedDevice: !suspicious,
    suspicious,
    revokedAt: null,
    revokedBy: null,
    userAgent: `${browser}/121 (${os}; ${device})`,
    activityTimeline: [
      'Authenticated successfully',
      'Loaded dashboard overview',
      'Queried admin directory'
    ]
  };
}

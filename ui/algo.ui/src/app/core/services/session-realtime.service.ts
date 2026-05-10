import { Injectable, NgZone, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import * as signalR from '@microsoft/signalr';

import { AppConfigService } from '../config/app-config.service';
import { AppToastService } from './app-toast.service';
import { AuthService } from './auth.service';

export interface OperationalActivityEvent {
  readonly timestamp: string;
  readonly level: 'info' | 'warn' | 'error' | string;
  readonly source: string;
  readonly message: string;
  readonly traceId?: string | null;
  readonly userId?: string | null;
  readonly statusCode?: number | null;
  readonly durationMs?: number | null;
}

@Injectable({ providedIn: 'root' })
export class SessionRealtimeService {
  private readonly auth = inject(AuthService);
  private readonly config = inject(AppConfigService);
  private readonly toast = inject(AppToastService);
  private readonly router = inject(Router);
  private readonly ngZone = inject(NgZone);

  private readonly onlineUserIdsState = signal<Set<string>>(new Set<string>());
  private readonly operationalActivityState = signal<OperationalActivityEvent[]>([]);
  private readonly connectedState = signal(false);
  private connection: signalR.HubConnection | null = null;

  readonly onlineUserIds = computed(() => this.onlineUserIdsState());
  readonly operationalActivity = computed(() => this.operationalActivityState());
  readonly isConnected = computed(() => this.connectedState());

  start(): void {
    if (!this.auth.isAuthenticated() || this.connection) {
      return;
    }

    const hubUrl = `${this.config.apiBaseUrl().replace(/\/api\/?$/, '')}/hubs/sessions`;
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, {
        accessTokenFactory: () => this.auth.getAccessToken() ?? ''
      })
      .withAutomaticReconnect()
      .build();

    connection.on('forceLogout', (payload?: { reason?: string }) => {
      this.ngZone.run(() => {
        this.auth.clearSession();
        this.stop();
        this.toast.warn('Session ended', payload?.reason ?? 'Your session was ended by administrator.');
        void this.router.navigateByUrl('/auth/login');
      });
    });

    connection.on('presenceSnapshot', (payload?: { onlineUserIds?: string[] }) => {
      this.ngZone.run(() => {
        const ids = payload?.onlineUserIds ?? [];
        this.onlineUserIdsState.set(new Set(ids));
      });
    });

    connection.on('userPresenceChanged', (payload?: { userId?: string; isOnline?: boolean }) => {
      if (!payload?.userId) {
        return;
      }

      this.ngZone.run(() => {
        this.onlineUserIdsState.update((current) => {
          const updated = new Set(current);
          if (payload.isOnline) {
            updated.add(payload.userId!);
          } else {
            updated.delete(payload.userId!);
          }

          return updated;
        });
      });
    });

    connection.on('operationalActivity', (payload?: OperationalActivityEvent) => {
      if (!payload) {
        return;
      }

      this.ngZone.run(() => this.pushOperationalActivity(payload));
    });

    connection.onreconnecting(() => {
      this.ngZone.run(() => {
        this.connectedState.set(false);
        this.pushOperationalActivity({
          timestamp: new Date().toISOString(),
          level: 'warn',
          source: 'websocket',
          message: 'Live operations channel is reconnecting.'
        });
      });
    });

    connection.onreconnected(() => {
      this.ngZone.run(() => {
        this.connectedState.set(true);
        this.pushOperationalActivity({
          timestamp: new Date().toISOString(),
          level: 'info',
          source: 'websocket',
          message: 'Live operations channel reconnected.'
        });
      });
    });

    connection.onclose(() => {
      this.ngZone.run(() => this.connectedState.set(false));
    });

    this.connection = connection;
    void connection.start().then(() => {
      this.ngZone.run(() => {
        this.connectedState.set(true);
        this.pushOperationalActivity({
          timestamp: new Date().toISOString(),
          level: 'info',
          source: 'websocket',
          message: 'Live operations channel connected.'
        });
      });
    });
  }

  stop(): void {
    const current = this.connection;
    this.connection = null;
    this.onlineUserIdsState.set(new Set<string>());
    this.connectedState.set(false);

    if (current) {
      void current.stop();
    }
  }

  clearOperationalActivity(): void {
    this.operationalActivityState.set([]);
  }

  private pushOperationalActivity(activity: OperationalActivityEvent): void {
    this.operationalActivityState.update((events) => [activity, ...events].slice(0, 150));
  }
}

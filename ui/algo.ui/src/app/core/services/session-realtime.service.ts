import { Injectable, NgZone, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import * as signalR from '@microsoft/signalr';

import { AppConfigService } from '../config/app-config.service';
import { AppToastService } from './app-toast.service';
import { AuthService } from './auth.service';

@Injectable({ providedIn: 'root' })
export class SessionRealtimeService {
  private readonly auth = inject(AuthService);
  private readonly config = inject(AppConfigService);
  private readonly toast = inject(AppToastService);
  private readonly router = inject(Router);
  private readonly ngZone = inject(NgZone);

  private readonly onlineUserIdsState = signal<Set<string>>(new Set<string>());
  private connection: signalR.HubConnection | null = null;

  readonly onlineUserIds = computed(() => this.onlineUserIdsState());

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

    this.connection = connection;
    void connection.start();
  }

  stop(): void {
    const current = this.connection;
    this.connection = null;
    this.onlineUserIdsState.set(new Set<string>());

    if (current) {
      void current.stop();
    }
  }
}

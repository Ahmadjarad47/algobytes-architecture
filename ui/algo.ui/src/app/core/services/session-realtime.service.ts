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

export interface RealtimeChatMessage {
  readonly id: string;
  readonly senderUserId: string;
  readonly recipientUserId: string;
  readonly senderDisplayName: string;
  readonly senderIsAdmin: boolean;
  readonly content: string;
  readonly sentAtUtc: string;
  readonly replyToMessageId?: string | null;
  readonly isRead: boolean;
  readonly readAtUtc?: string | null;
}

export interface RealtimeTypingEvent {
  readonly userId: string;
  readonly displayName: string;
  readonly isAdmin: boolean;
  readonly isTyping: boolean;
  readonly timestampUtc: string;
}

export interface RealtimeChatReadReceipt {
  readonly readerUserId: string;
  readonly counterpartUserId: string;
  readonly messageIds: string[];
  readonly readAtUtc: string;
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
  private readonly directChatByUserState = signal<Map<string, RealtimeChatMessage[]>>(new Map());
  private readonly typingByUserState = signal<Map<string, Map<string, RealtimeTypingEvent>>>(new Map());
  private readonly unreadCountsState = signal<Map<string, number>>(new Map());
  private readonly connectedState = signal(false);
  private connection: signalR.HubConnection | null = null;

  readonly onlineUserIds = computed(() => this.onlineUserIdsState());
  readonly operationalActivity = computed(() => this.operationalActivityState());
  readonly directChatByUser = computed(() => this.directChatByUserState());
  readonly unreadCounts = computed(() => this.unreadCountsState());
  readonly totalUnreadCount = computed(() =>
    Array.from(this.unreadCountsState().values()).reduce((sum, count) => sum + count, 0)
  );
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

    connection.on('directChatHistory', (payload?: { targetUserId?: string; messages?: RealtimeChatMessage[] }) => {
      if (!payload?.targetUserId) {
        return;
      }
      this.ngZone.run(() => this.directChatByUserState.update((current) => {
        const next = new Map(current);
        const items = [...(payload.messages ?? [])];
        items.sort((a, b) => new Date(a.sentAtUtc).getTime() - new Date(b.sentAtUtc).getTime());
        next.set(payload.targetUserId!, items);
        this.recomputeUnreadCounts(next);
        return next;
      }));
    });

    connection.on('directChatMessage', (payload?: RealtimeChatMessage) => {
      if (!payload) {
        return;
      }

      this.ngZone.run(() => {
        const currentUserId = this.auth.session()?.user?.userId;
        if (!currentUserId) {
          return;
        }

        const counterpartUserId = payload.senderUserId === currentUserId ? payload.recipientUserId : payload.senderUserId;
        this.directChatByUserState.update((current) => {
          const next = new Map(current);
          const existing = next.get(counterpartUserId) ?? [];
          next.set(counterpartUserId, [...existing, payload].slice(-250));
          this.recomputeUnreadCounts(next);
          const isAdmin = (this.auth.session()?.user?.roles ?? []).some((role) => role.toLowerCase() === 'admin');
          const isIncoming = payload.recipientUserId === currentUserId && payload.senderUserId !== currentUserId;
          if (isAdmin && isIncoming) {
            this.toast.info('New message', `${payload.senderDisplayName}: ${payload.content.slice(0, 60)}`);
          }
          return next;
        });
      });
    });

    connection.on('directChatTyping', (payload?: { targetUserId?: string; eventData?: RealtimeTypingEvent }) => {
      if (!payload?.targetUserId || !payload.eventData?.userId) {
        return;
      }

      this.ngZone.run(() => {
        this.typingByUserState.update((current) => {
          const updated = new Map(current);
          const key = payload.targetUserId!;
          const userTypingMap = new Map(updated.get(key) ?? new Map<string, RealtimeTypingEvent>());
          if (payload.eventData!.isTyping) {
            userTypingMap.set(payload.eventData!.userId, payload.eventData!);
          } else {
            userTypingMap.delete(payload.eventData!.userId);
          }
          updated.set(key, userTypingMap);
          return updated;
        });
      });
    });

    connection.on('directChatRead', (payload?: RealtimeChatReadReceipt) => {
      if (!payload?.messageIds?.length) {
        return;
      }

      this.ngZone.run(() => {
        this.directChatByUserState.update((current) => {
          const next = new Map(current);
          const list = next.get(payload.counterpartUserId);
          if (!list?.length) {
            return next;
          }

          const updated = list.map((item) =>
            payload.messageIds.includes(item.id)
              ? { ...item, isRead: true, readAtUtc: payload.readAtUtc }
              : item
          );
          next.set(payload.counterpartUserId, updated);
          this.recomputeUnreadCounts(next);
          return next;
        });
      });
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
    this.directChatByUserState.set(new Map());
    this.typingByUserState.set(new Map());
    this.unreadCountsState.set(new Map());
    this.connectedState.set(false);

    if (current) {
      void current.stop();
    }
  }

  clearOperationalActivity(): void {
    this.operationalActivityState.set([]);
  }

  async loadDirectChatHistory(targetUserId: string): Promise<void> {
    if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) {
      throw new Error('Live chat is not connected.');
    }
    await this.connection.invoke('LoadDirectChatHistory', targetUserId);
  }

  async markDirectConversationRead(targetUserId: string): Promise<void> {
    if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) {
      return;
    }

    await this.connection.invoke('MarkDirectConversationRead', targetUserId);
  }

  async sendDirectMessage(targetUserId: string, content: string, replyToMessageId?: string | null): Promise<void> {
    if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) {
      throw new Error('Live chat is not connected.');
    }
    await this.connection.invoke('SendDirectMessage', targetUserId, content, replyToMessageId ?? null);
  }

  async setDirectTyping(targetUserId: string, isTyping: boolean): Promise<void> {
    if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) {
      return;
    }
    await this.connection.invoke('SetDirectTyping', targetUserId, isTyping);
  }

  getDirectMessages(targetUserId: string): RealtimeChatMessage[] {
    return this.directChatByUserState().get(targetUserId) ?? [];
  }

  getTypingUsersForTarget(targetUserId: string): RealtimeTypingEvent[] {
    return Array.from((this.typingByUserState().get(targetUserId) ?? new Map()).values());
  }

  getUnreadCountForUser(targetUserId: string): number {
    return this.unreadCountsState().get(targetUserId) ?? 0;
  }

  private pushOperationalActivity(activity: OperationalActivityEvent): void {
    this.operationalActivityState.update((events) => [activity, ...events].slice(0, 150));
  }

  private recomputeUnreadCounts(chatMap: Map<string, RealtimeChatMessage[]>): void {
    const currentUserId = this.auth.session()?.user?.userId;
    if (!currentUserId) {
      this.unreadCountsState.set(new Map());
      return;
    }

    const unread = new Map<string, number>();
    for (const [counterpartId, messages] of chatMap.entries()) {
      const count = messages.filter((message) => message.recipientUserId === currentUserId && !message.isRead).length;
      if (count > 0) {
        unread.set(counterpartId, count);
      }
    }

    this.unreadCountsState.set(unread);
  }
}

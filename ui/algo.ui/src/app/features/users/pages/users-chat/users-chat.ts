import { ChangeDetectionStrategy, Component, computed, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';

import { AppToastService } from '../../../../core/services/app-toast.service';
import { AuthService } from '../../../../core/services/auth.service';
import {
  RealtimeChatMessage,
  SessionRealtimeService
} from '../../../../core/services/session-realtime.service';
import { UsersApiService } from '../../api/users-api.service';
import { UserListItem } from '../../models/users.models';

@Component({
  selector: 'app-users-chat',
  imports: [FormsModule, RouterLink, RouterLinkActive, InputTextModule, ButtonModule],
  template: `
    <section class="surface-card dashboard-section users-chat-shell">
      <div class="mb-3 flex flex-wrap items-center gap-2 border-b border-slate-200 pb-2">
        <a
          routerLink="/users/directory"
          routerLinkActive="bg-slate-900 text-white"
          class="rounded-full px-3 py-1.5 text-xs font-semibold text-slate-600 transition hover:bg-slate-100"
        >
          Directory
        </a>
        <a
          routerLink="/users/chat"
          routerLinkActive="bg-slate-900 text-white"
          class="rounded-full px-3 py-1.5 text-xs font-semibold text-slate-600 transition hover:bg-slate-100"
        >
          Chat
        </a>
      </div>

      <div class="grid grid-cols-1 gap-3 md:grid-cols-[260px_1fr]">
        <div class="rounded-2xl border border-slate-200 bg-slate-50 p-2 shadow-sm">
          <div class="mb-2 text-[12px] font-semibold text-slate-700">Users</div>
          <div class="max-h-[58vh] overflow-auto">
            @for (user of chatUsers(); track user.id) {
              <button
                type="button"
                class="mb-1 w-full rounded-xl border px-2.5 py-2 text-left text-[12px] transition"
                [class]="selectedChatUserId() === user.id ? 'border-slate-900 bg-slate-900 text-white shadow-md' : 'border-slate-200 bg-white text-slate-700 hover:border-slate-300'"
                (click)="selectChatUser(user.id)"
              >
                <div class="flex items-center justify-between gap-2">
                  <div class="font-semibold">{{ user.displayName }}</div>
                  @if (unreadFor(user.id) > 0) {
                    <span class="rounded-full bg-emerald-500 px-1.5 py-0.5 text-[10px] font-bold text-white">{{ unreadFor(user.id) }}</span>
                  }
                </div>
                <div class="text-[11px] opacity-80">{{ user.email ?? user.userName ?? user.id }}</div>
              </button>
            } @empty {
              <div class="text-[12px] text-slate-500">No users found.</div>
            }
          </div>
        </div>

        <div class="grid gap-3">
          <div class="text-[12px] text-slate-600">
            @if (typingUsersLabel()) {
              <span>{{ typingUsersLabel() }}</span>
            } @else {
              <span>No one is typing.</span>
            }
          </div>

          <div class="max-h-[52vh] overflow-auto rounded-2xl border border-slate-200 bg-gradient-to-b from-slate-50 to-white p-3">
            @for (message of activeChatMessages(); track message.id) {
              <div class="mb-2 flex last:mb-0" [class.justify-end]="isMine(message)" [class.justify-start]="!isMine(message)">
                <div class="chat-bubble w-[min(78%,44rem)] rounded-2xl border p-2.5 shadow-sm"
                  [class]="isMine(message) ? 'border-emerald-300 bg-emerald-50 text-slate-900' : 'border-slate-200 bg-white text-slate-900'">
                  <div class="mb-1 flex items-center justify-between gap-2">
                    <div class="text-[12px] font-semibold">{{ message.senderDisplayName }}</div>
                    <div class="text-[11px] text-slate-500">{{ formatActivityTime(message.sentAtUtc) }}</div>
                  </div>
                  @if (message.replyToMessageId && findChatMessageById(message.replyToMessageId); as repliedTo) {
                    <div class="mb-1 rounded-lg border-l-2 border-slate-300 bg-slate-50 px-2 py-1 text-[11px] text-slate-600">
                      Reply to {{ repliedTo.senderDisplayName }}: {{ repliedTo.content }}
                    </div>
                  }
                  <div class="whitespace-pre-wrap text-[13px] leading-6">{{ message.content }}</div>
                  <div class="mt-1.5 flex items-center justify-between">
                    <button
                      type="button"
                      class="text-[11px] font-medium text-slate-600 hover:text-slate-900"
                      (click)="startReply(message.id)"
                    >
                      Reply
                    </button>
                    @if (isMine(message)) {
                      <span class="text-[10px] text-slate-500">
                        {{ message.isRead ? 'Read ' + formatReadTime(message.readAtUtc) : 'Sent' }}
                      </span>
                    }
                  </div>
                </div>
              </div>
            } @empty {
              <div class="py-10 text-center text-[12px] text-slate-500">Select a user and start chatting.</div>
            }
          </div>

          @if (replyingToMessage(); as replyId) {
            @if (findChatMessageById(replyId); as replyMessage) {
              <div class="rounded-xl border border-slate-300 bg-slate-50 px-3 py-2 text-[11px] text-slate-700">
                Replying to {{ replyMessage.senderDisplayName }}: {{ replyMessage.content }}
              </div>
            }
          }

          <div class="grid gap-2">
            <input
              pInputText
              [(ngModel)]="chatDraft"
              (input)="onChatInput()"
              (keydown.enter)="submitChatMessage()"
              placeholder="Type your message and press Enter"
              class="w-full"
            />
            <div class="flex justify-end gap-2">
              <p-button
                icon="pi pi-times"
                label="Cancel Reply"
                size="small"
                severity="secondary"
                [outlined]="true"
                [disabled]="!replyingToMessage()"
                (onClick)="cancelReply()"
              />
              <p-button
                icon="pi pi-send"
                label="Send"
                size="small"
                [disabled]="!chatDraft.trim() || !selectedChatUserId()"
                (onClick)="submitChatMessage()"
              />
            </div>
          </div>
        </div>
      </div>
    </section>
  `,
  styles: [`
    .users-chat-shell {
      background: linear-gradient(180deg, #ffffff 0%, #f8fafc 100%);
    }
    .chat-bubble {
      backdrop-filter: blur(2px);
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class UsersChat {
  private readonly sessionRealtime = inject(SessionRealtimeService);
  private readonly usersApi = inject(UsersApiService);
  private readonly toast = inject(AppToastService);
  private readonly auth = inject(AuthService);

  protected readonly chatUsers = signal<UserListItem[]>([]);
  protected readonly selectedChatUserId = signal<string | null>(null);
  protected readonly replyingToMessage = signal<string | null>(null);
  protected chatDraft = '';
  private typingTimeoutHandle: ReturnType<typeof setTimeout> | null = null;
  private lastAutoReadKey: string | null = null;

  protected readonly activeChatMessages = computed(() => {
    const selected = this.selectedChatUserId();
    return selected ? this.sessionRealtime.getDirectMessages(selected) : [];
  });

  protected readonly typingUsersLabel = computed(() => {
    const selected = this.selectedChatUserId();
    if (!selected) {
      return '';
    }

    const currentUserId = this.auth.session()?.user?.userId;
    const typing = this.sessionRealtime
      .getTypingUsersForTarget(selected)
      .filter((item) => item.userId !== currentUserId);

    if (!typing.length) {
      return '';
    }

    const labels = typing.slice(0, 2).map((item) => item.displayName);
    const suffix = typing.length > 2 ? ` +${typing.length - 2} more` : '';
    return `${labels.join(', ')} ${typing.length > 1 ? 'are' : 'is'} typing${suffix}`;
  });

  constructor() {
    this.sessionRealtime.start();
    void this.loadChatUsers();

    effect(() => {
      const selected = this.selectedChatUserId();
      if (!selected) {
        return;
      }

      const currentUserId = this.auth.session()?.user?.userId;
      if (!currentUserId) {
        return;
      }

      const unreadIncoming = this.activeChatMessages().find((message) =>
        message.recipientUserId === currentUserId && !message.isRead
      );

      if (!unreadIncoming) {
        return;
      }

      const key = `${selected}:${unreadIncoming.id}`;
      if (this.lastAutoReadKey === key) {
        return;
      }

      this.lastAutoReadKey = key;
      void this.sessionRealtime.markDirectConversationRead(selected);
    });
  }

  protected async selectChatUser(userId: string): Promise<void> {
    this.selectedChatUserId.set(userId);
    await this.sessionRealtime.loadDirectChatHistory(userId);
    await this.sessionRealtime.markDirectConversationRead(userId);
  }

  protected onChatInput(): void {
    const selected = this.selectedChatUserId();
    if (!selected) {
      return;
    }

    void this.sessionRealtime.setDirectTyping(selected, Boolean(this.chatDraft.trim()));

    if (this.typingTimeoutHandle) {
      clearTimeout(this.typingTimeoutHandle);
    }

    this.typingTimeoutHandle = setTimeout(() => {
      void this.sessionRealtime.setDirectTyping(selected, false);
    }, 1800);
  }

  protected async submitChatMessage(): Promise<void> {
    const content = this.chatDraft.trim();
    if (!content) {
      return;
    }

    const selected = this.selectedChatUserId();
    if (!selected) {
      this.toast.warn('Select user', 'Please select a user first.');
      return;
    }

    try {
      await this.sessionRealtime.sendDirectMessage(selected, content, this.replyingToMessage());
      this.chatDraft = '';
      this.replyingToMessage.set(null);
      if (this.typingTimeoutHandle) {
        clearTimeout(this.typingTimeoutHandle);
      }
      await this.sessionRealtime.setDirectTyping(selected, false);
    } catch {
      this.toast.error('Chat send failed', 'Unable to send your message right now.');
    }
  }

  protected startReply(messageId: string): void {
    this.replyingToMessage.set(messageId);
  }

  protected cancelReply(): void {
    this.replyingToMessage.set(null);
  }

  protected findChatMessageById(messageId: string | null | undefined): RealtimeChatMessage | null {
    if (!messageId) {
      return null;
    }

    return this.activeChatMessages().find((item) => item.id === messageId) ?? null;
  }

  protected formatActivityTime(value: string): string {
    return new Date(value).toLocaleTimeString([], {
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit'
    });
  }

  protected formatReadTime(value: string | null | undefined): string {
    if (!value) {
      return '';
    }

    return new Date(value).toLocaleTimeString([], {
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  protected isMine(message: RealtimeChatMessage): boolean {
    return message.senderUserId === this.auth.session()?.user?.userId;
  }

  protected unreadFor(userId: string): number {
    return this.sessionRealtime.getUnreadCountForUser(userId);
  }

  private async loadChatUsers(): Promise<void> {
    try {
      const page = await firstValueFrom(this.usersApi.getUsers({ PageNumber: 1, PageSize: 100 }));
      const currentUserId = this.auth.session()?.user?.userId;
      const users = page.items.filter((item) => item.id !== currentUserId);
      this.chatUsers.set(users);

      if (users.length) {
        await this.selectChatUser(users[0].id);
      }
    } catch {
      this.toast.error('Users load failed', 'Unable to load users for chat.');
    }
  }
}

import { ChangeDetectionStrategy, Component, HostListener, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { DividerModule } from 'primeng/divider';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { InputTextModule } from 'primeng/inputtext';
import { MenuModule } from 'primeng/menu';
import { TagModule } from 'primeng/tag';
import { TooltipModule } from 'primeng/tooltip';
import { MenuItem } from 'primeng/api';
import { firstValueFrom } from 'rxjs';
import { AppConfigService } from '../../core/config/app-config.service';
import { DashboardOverviewReadPermissions, Permissions } from '../../core/permissions/permission.catalog';
import { PermissionService } from '../../core/permissions/permission.service';
import { PermissionGate } from '../../core/permissions/permission.types';
import { AdminActionBusService, AdminGlobalAction } from '../../core/services/admin-action-bus.service';
import { AppToastService } from '../../core/services/app-toast.service';
import { AuthService } from '../../core/services/auth.service';
import { SessionRealtimeService } from '../../core/services/session-realtime.service';
import { ThemeService } from '../../core/services/theme.service';
import { AuthFacadeService } from '../../features/auth/services/auth-facade.service';
import { UsersApiService } from '../../features/users/api/users-api.service';
import { UserListItem } from '../../features/users/models/users.models';

@Component({
  selector: 'app-dashboard-layout',
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    FormsModule,
    ButtonModule,
    DialogModule,
    DividerModule,
    IconFieldModule,
    InputIconModule,
    InputTextModule,
    MenuModule,
    TagModule,
    TooltipModule
  ],
  template: `
    <main class="density-compact min-h-dvh text-surface-950">
      <div class="app-shell flex min-h-dvh flex-col gap-2 py-2 lg:flex-row">
        <aside
          class="app-sidebar surface-sidebar flex w-full flex-col rounded-2xl px-2.5 py-2.5 lg:sticky lg:top-2 lg:h-[calc(100dvh-1rem)] lg:w-[var(--app-sidebar-width)] lg:flex-none"
          [class.is-sidebar-collapsed]="sidebarCollapsed()"
        >
          <div class="flex items-center justify-between gap-2">
            <div class="flex min-w-0 items-center gap-2.5">
              <div
                class="flex h-8 w-8 items-center justify-center rounded-xl bg-slate-950 text-[11px] font-semibold text-white"
              >
                A
              </div>
              @if (!sidebarCollapsed()) {
                <div class="min-w-0">
                  <div class="eyebrow">{{ config().appName }} workspace</div>
                  <div class="truncate text-sm font-semibold text-surface-950">{{ config().sidebarTitle }}</div>
                </div>
              }
            </div>

            <button
              pButton
              type="button"
              [icon]="sidebarCollapsed() ? 'pi pi-angle-double-right' : 'pi pi-angle-double-left'"
              severity="secondary"
              [text]="true"
              [rounded]="true"
              size="small"
              [attr.aria-label]="sidebarCollapsed() ? 'Expand navigation' : 'Collapse navigation'"
              [pTooltip]="sidebarCollapsed() ? 'Expand navigation' : 'Collapse navigation'"
              tooltipPosition="left"
              (click)="toggleSidebar()"
            ></button>
          </div>

          <div class="app-sidebar-profile mt-3 rounded-xl border border-slate-200/80 bg-slate-50/80 p-2.5">
            <div class="flex items-center gap-2">
              <div
                class="flex h-8 w-8 flex-none items-center justify-center rounded-lg bg-white text-xs font-semibold text-slate-700 shadow-sm"
              >
                {{ initials() }}
              </div>
              @if (!sidebarCollapsed()) {
                <div class="min-w-0 flex-1">
                  <div class="truncate text-sm font-semibold text-slate-900">{{ displayName() }}</div>
                  <div class="truncate text-[11px] text-slate-500">{{ email() }}</div>
                </div>
                <p-tag value="Prod" severity="contrast" />
              }
            </div>
          </div>

          <div class="app-sidebar-nav mt-3 rounded-xl border border-slate-200/80 bg-white/70 p-2">
            @if (!sidebarCollapsed()) {
              <div class="mb-2 flex items-center justify-between px-1">
                <div class="text-[11px] font-semibold uppercase tracking-wide text-slate-500">
                  Navigation
                </div>
                <span class="text-[10px] text-slate-400">{{ navigation().length }} modules</span>
              </div>
            }

            <nav class="flex flex-col gap-1">
              @for (item of navigation(); track item.path) {
                <a
                  [routerLink]="item.path"
                  routerLinkActive="bg-slate-950 text-white shadow-sm"
                  #navLink="routerLinkActive"
                  class="app-sidebar-link flex items-center gap-2 rounded-xl px-2.5 py-2 text-[12px] font-medium text-slate-700 transition hover:bg-slate-100"
                  [attr.aria-label]="item.label"
                  [pTooltip]="sidebarCollapsed() ? item.label : ''"
                  tooltipPosition="right"
                >
                  <span
                    class="flex h-7 w-7 items-center justify-center rounded-lg"
                    [class]="navLink.isActive ? 'bg-white/12 text-white' : 'bg-slate-100 text-slate-500'"
                  >
                    <i [class]="item.icon" class="text-[11px]"></i>
                  </span>
                  @if (!sidebarCollapsed()) {
                    <span class="flex-1 truncate">{{ item.label }}</span>
                  }
                  @if (item.badge && !sidebarCollapsed()) {
                    <span
                      class="rounded-full px-1.5 py-0.5 text-[10px] font-semibold"
                      [class]="navLink.isActive ? 'bg-white/12 text-white' : 'bg-slate-200 text-slate-600'"
                    >
                      {{ item.badge }}
                    </span>
                  }
                </a>
              }
            </nav>
          </div>

          <div class="app-sidebar-footer mt-auto pt-3">
            <div class="rounded-xl border border-slate-200/80 bg-slate-50/80 p-2.5">
              @if (!sidebarCollapsed()) {
                <div class="mb-1.5 text-[11px] font-semibold text-slate-800">Workspace health</div>
                <div class="flex items-center justify-between text-[11px] text-slate-500">
                  <span>API sync</span>
                  <span class="font-medium text-emerald-600">Connected</span>
                </div>
                <div class="mt-1.5 flex items-center justify-between text-[11px] text-slate-500">
                  <span>Last refresh</span>
                  <span class="font-medium text-slate-700">Live</span>
                </div>
              }
              <p-button
  styleClass="app-theme-toggle"
  [icon]="theme.isDark() ? 'pi pi-sun' : 'pi pi-moon'"
  severity="secondary"
  [rounded]="true"
  [text]="true"
  [pTooltip]="theme.isDark() ? 'Switch to light mode' : 'Switch to dark mode'"
  tooltipPosition="left"
  [attr.aria-label]="theme.isDark() ? 'Switch to light mode' : 'Switch to dark mode'"
  (onClick)="theme.toggle()"
/>
              <div class="mt-2 flex gap-1.5">
                <p-button
                  icon="pi pi-cog"
                  [label]="sidebarCollapsed() ? undefined : 'Settings'"
                  severity="secondary"
                  size="small"
                  [outlined]="true"
                  [pTooltip]="sidebarCollapsed() ? 'Settings' : ''"
                  tooltipPosition="right"
                  [routerLink]="['/settings']"
                />
                <p-button
                  icon="pi pi-sign-out"
                  severity="secondary"
                  size="small"
                  [text]="true"
                  pTooltip="Log out"
                  tooltipPosition="top"
                  (onClick)="logout()"
                />
              </div>
            </div>
          </div>
        </aside>

        <section class="surface-content min-w-0 flex-1 rounded-2xl px-2.5 py-2.5 lg:px-3 lg:py-3">
          <div class="content-shell mx-auto flex min-h-full flex-col gap-3">
            <header class="surface-card rounded-2xl px-3 py-2.5">
              <div class="flex flex-col gap-2 lg:flex-row lg:items-center lg:justify-between">
                <div class="min-w-0">
                  <div class="mb-1 flex flex-wrap items-center gap-1.5 text-[11px] text-slate-500">
                    <span>Console</span>
                    <i class="pi pi-angle-right text-[9px]"></i>
                    <span class="font-medium text-slate-700">{{ activeSection().section }}</span>
                    @if (activeSection().page) {
                      <i class="pi pi-angle-right text-[9px]"></i>
                      <span class="font-medium text-slate-900">{{ activeSection().page }}</span>
                    }
                  </div>
                  <div class="flex items-center gap-2">
                    <h1 class="m-0 truncate text-[17px] font-semibold leading-tight text-slate-950">
                      {{ activeSection().title }}
                    </h1>
                    <p-tag [value]="activeSection().badge" severity="secondary" />
                  </div>
                </div>

                <div class="flex flex-col gap-2 sm:flex-row sm:items-center">
                  <p-iconfield>
                    <p-inputicon class="pi pi-search" />
                    <input pInputText placeholder="Search users, logs, roles" class="w-full sm:w-64" />
                  </p-iconfield>

                  <div class="flex flex-wrap items-center gap-1.5">
                    <p-button
                      icon="pi pi-bolt"
                      label="Quick action"
                      severity="secondary"
                      size="small"
                      [outlined]="true"
                      (onClick)="openCommandPalette()"
                    />
                    <p-button
                      icon="pi pi-terminal"
                      label="Live console"
                      severity="secondary"
                      size="small"
                      [outlined]="true"
                      [badge]="activityCountLabel()"
                      (onClick)="openOperationalConsole()"
                    />
                    <p-button icon="pi pi-plus" label="New" size="small" (onClick)="newMenu.toggle($event)" />
                    <p-menu #newMenu [model]="newItems()" [popup]="true" appendTo="body" />
                  </div>
                </div>
              </div>
            </header>

            <router-outlet />
          </div>
        </section>
      </div>

      <p-dialog
        [visible]="commandPaletteVisible()"
        header="Quick action"
        [modal]="true"
        [draggable]="false"
        [resizable]="false"
        [style]="{ width: 'min(40rem, 94vw)' }"
        styleClass="surface-dialog"
        (visibleChange)="commandPaletteVisible.set($event)"
      >
        <div class="grid gap-3">
          <p-iconfield>
            <p-inputicon class="pi pi-search" />
            <input
              pInputText
              [(ngModel)]="commandSearch"
              placeholder="Search commands"
              class="w-full"
            />
          </p-iconfield>

          <div class="grid gap-1.5">
            @for (command of filteredCommands(); track command.id) {
              <button
                type="button"
                class="flex w-full items-center gap-3 rounded-xl border border-slate-200 bg-slate-50 px-3 py-2 text-left transition hover:bg-white"
                (click)="runCommand(command.id)"
              >
                <span class="flex h-8 w-8 items-center justify-center rounded-lg bg-white text-slate-600 shadow-sm">
                  <i [class]="command.icon" class="text-[12px]"></i>
                </span>
                <span class="min-w-0">
                  <span class="block text-[12px] font-semibold text-slate-900">{{ command.label }}</span>
                  <span class="block text-[11px] text-slate-500">{{ command.hint }}</span>
                </span>
              </button>
            } @empty {
              <div class="rounded-xl border border-slate-200 bg-slate-50 px-3 py-4 text-center text-[12px] text-slate-500">
                No matching commands.
              </div>
            }
          </div>
        </div>
      </p-dialog>

      <p-dialog
        [visible]="chatConsoleVisible()"
        header="Direct Messages"
        [modal]="false"
        [draggable]="true"
        [resizable]="true"
        [style]="{ width: 'min(72rem, 96vw)' }"
        styleClass="surface-dialog"
        (visibleChange)="chatConsoleVisible.set($event); onChatDialogVisibilityChange($event)"
      >
        <div class="grid grid-cols-1 gap-3 md:grid-cols-[240px_1fr]">
          <div class="rounded-xl border border-slate-200 bg-slate-50 p-2">
            <div class="mb-2 text-[12px] font-semibold text-slate-700">Users</div>
            <div class="max-h-[56vh] overflow-auto">
              @for (user of chatUsers(); track user.id) {
                <button
                  type="button"
                  class="mb-1 w-full rounded-lg border px-2.5 py-2 text-left text-[12px]"
                  [class]="selectedChatUserId() === user.id ? 'border-slate-900 bg-slate-900 text-white' : 'border-slate-200 bg-white text-slate-700'"
                  (click)="selectChatUser(user.id)"
                >
                  <div class="font-semibold">{{ user.displayName }}</div>
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
            <div class="max-h-[48vh] overflow-auto rounded-xl border border-slate-200 bg-slate-50/60 p-3">
              @for (message of activeChatMessages(); track message.id) {
                <div class="mb-2 rounded-xl border border-slate-200 bg-white p-2.5 last:mb-0">
                  <div class="mb-1 flex items-center justify-between gap-2">
                    <div class="text-[12px] font-semibold text-slate-900">{{ message.senderDisplayName }}</div>
                    <div class="text-[11px] text-slate-500">{{ formatActivityTime(message.sentAtUtc) }}</div>
                  </div>
                  @if (message.replyToMessageId && findChatMessageById(message.replyToMessageId); as repliedTo) {
                    <div class="mb-1 rounded-lg border-l-2 border-slate-300 bg-slate-50 px-2 py-1 text-[11px] text-slate-600">
                      Reply to {{ repliedTo.senderDisplayName }}: {{ repliedTo.content }}
                    </div>
                  }
                  <div class="whitespace-pre-wrap text-[13px] text-slate-800">{{ message.content }}</div>
                  <div class="mt-1.5">
                    <button type="button" class="text-[11px] font-medium text-slate-600 hover:text-slate-900" (click)="startReply(message.id)">Reply</button>
                  </div>
                </div>
              } @empty {
                <div class="py-8 text-center text-[12px] text-slate-500">Select a user and start chatting.</div>
              }
            </div>
            <div class="grid gap-2">
              <input pInputText [(ngModel)]="chatDraft" (input)="onChatInput()" (keydown.enter)="submitChatMessage()" placeholder="Type your message and press Enter" class="w-full" />
              <div class="flex justify-end">
                <p-button icon="pi pi-send" label="Send" size="small" [disabled]="!chatDraft.trim() || !selectedChatUserId()" (onClick)="submitChatMessage()" />
              </div>
            </div>
          </div>
        </div>
      </p-dialog>

      <p-dialog
        [visible]="operationalConsoleVisible()"
        header="Live Operational Console"
        [modal]="false"
        [draggable]="true"
        [resizable]="true"
        [style]="{ width: 'min(68rem, 94vw)' }"
        styleClass="surface-dialog operational-console-dialog"
        (visibleChange)="operationalConsoleVisible.set($event)"
      >
        <div class="grid gap-3">
          <div class="flex flex-wrap items-center justify-between gap-2">
            <div class="flex items-center gap-2">
              <span
                class="h-2.5 w-2.5 rounded-full"
                [class]="sessionRealtime.isConnected() ? 'bg-emerald-400 shadow-[0_0_14px_rgba(52,211,153,.85)]' : 'bg-amber-400'"
              ></span>
              <span class="text-[12px] font-semibold text-slate-200">
                {{ sessionRealtime.isConnected() ? 'stream online' : 'stream reconnecting' }}
              </span>
              <p-tag [value]="operationalActivity().length + ' events'" severity="secondary" />
            </div>
            <p-button
              icon="pi pi-trash"
              label="Clear"
              severity="secondary"
              size="small"
              [outlined]="true"
              (onClick)="clearOperationalConsole()"
            />
          </div>

          <div class="max-h-[62vh] overflow-auto rounded-xl border border-slate-700 bg-[#05070a] p-3 font-mono text-[12px] shadow-inner">
            @for (event of operationalActivity(); track event.timestamp + event.message) {
              <div class="grid grid-cols-[88px_72px_90px_1fr] gap-3 border-b border-slate-800/80 py-1.5 last:border-b-0">
                <span class="text-slate-500">{{ formatActivityTime(event.timestamp) }}</span>
                <span [class]="activityLevelClass(event.level)">{{ event.level.toUpperCase() }}</span>
                <span class="text-cyan-300">{{ event.source }}</span>
                <span class="min-w-0 text-slate-200">
                  {{ event.message }}
                  @if (event.traceId) {
                    <span class="text-slate-500"> trace={{ event.traceId }}</span>
                  }
                </span>
              </div>
            } @empty {
              <div class="py-8 text-center text-slate-500">Waiting for websocket activity...</div>
            }
          </div>
        </div>
      </p-dialog>
    </main>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DashboardLayout {
  private readonly authService = inject(AuthService);
  private readonly authFacade = inject(AuthFacadeService);
  private readonly router = inject(Router);
  private readonly toast = inject(AppToastService);
  private readonly appConfig = inject(AppConfigService);
  private readonly permissionService = inject(PermissionService);
  private readonly actionBus = inject(AdminActionBusService);
  private readonly usersApi = inject(UsersApiService);
  protected readonly sessionRealtime = inject(SessionRealtimeService);
  protected readonly theme = inject(ThemeService);
  protected readonly config = this.appConfig.config;

  protected readonly sidebarCollapsed = signal(this.config().sidebarCollapsed);
  protected readonly commandPaletteVisible = signal(false);
  protected readonly operationalConsoleVisible = signal(false);
  protected readonly chatConsoleVisible = signal(false);
  protected readonly operationalActivity = this.sessionRealtime.operationalActivity;
  protected readonly chatUsers = signal<UserListItem[]>([]);
  protected readonly selectedChatUserId = signal<string | null>(null);
  protected readonly activeChatMessages = computed(() => {
    const selected = this.selectedChatUserId();
    if (!selected) {
      return [];
    }

    return this.sessionRealtime.getDirectMessages(selected);
  });
  protected readonly activityCountLabel = computed(() => {
    const count = this.operationalActivity().length;
    return count > 99 ? '99+' : count ? `${count}` : undefined;
  });
  protected readonly typingUsersLabel = computed(() => {
    const selected = this.selectedChatUserId();
    if (!selected) {
      return '';
    }
    const currentUserId = this.authService.session()?.user?.userId;
    const typing = this.sessionRealtime.getTypingUsersForTarget(selected).filter((item) => item.userId !== currentUserId);

    if (!typing.length) {
      return '';
    }

    const labels = typing.slice(0, 2).map((item) => item.displayName);
    const suffix = typing.length > 2 ? ` +${typing.length - 2} more` : '';
    return `${labels.join(', ')} ${typing.length > 1 ? 'are' : 'is'} typing${suffix}`;
  });
  protected readonly chatUnreadLabel = computed(() => {
    if (this.chatConsoleVisible()) {
      return undefined;
    }

    const count = Array.from(this.sessionRealtime.directChatByUser().values()).reduce((sum, items) => sum + items.length, 0);
    return count > 99 ? '99+' : count ? `${count}` : undefined;
  });
  protected commandSearch = '';
  protected chatDraft = '';
  protected readonly replyingToMessage = signal<string | null>(null);
  private typingTimeoutHandle: ReturnType<typeof setTimeout> | null = null;

  private readonly allNavigation: Array<{ label: string; path: string; icon: string; badge?: string; feature?: string; gate?: PermissionGate }> = [
    { label: 'Overview', path: '/dashboard', icon: 'pi pi-home', gate: { any: DashboardOverviewReadPermissions } },
    { label: 'Users', path: '/users', icon: 'pi pi-users', badge: 'IAM', feature: 'users', gate: { any: [Permissions.users.read] } },
    { label: 'Users Chat', path: '/users/chat', icon: 'pi pi-comments', feature: 'users', gate: { any: [Permissions.users.read] } },
    { label: 'Roles', path: '/roles', icon: 'pi pi-id-card', feature: 'roles', gate: { any: [Permissions.roles.read] } },
    { label: 'Access Policies', path: '/access-policies', icon: 'pi pi-shield', badge: 'Auth', feature: 'accessPolicies', gate: { any: [Permissions.accessPolicies.read] } },
    { label: 'Active Sessions', path: '/active-sessions', icon: 'pi pi-desktop', badge: 'Security', feature: 'activeSessions', gate: { any: [Permissions.sessions.read] } },
    { label: 'Logs', path: '/logs', icon: 'pi pi-list', feature: 'logs', gate: { any: [Permissions.logs.read] } },
    { label: 'Error Logs', path: '/error-logs', icon: 'pi pi-exclamation-circle', badge: 'Ops', feature: 'errorLogs', gate: { any: [Permissions.errorLogs.read] } },
    { label: 'Settings', path: '/settings', icon: 'pi pi-cog', feature: 'settings', gate: { any: [Permissions.settings.read] } }
  ];

  protected readonly navigation = computed(() => {
    const features = this.appConfig.features();
    const filtered = this.allNavigation.filter((item) =>
      (!item.feature || features[item.feature as keyof typeof features]) &&
      this.permissionService.can(item.gate));

    const unread = this.sessionRealtime.totalUnreadCount();
    return filtered.map((item) =>
      item.path === '/users/chat'
        ? { ...item, badge: unread > 99 ? '99+' : unread > 0 ? `${unread}` : undefined }
        : item);
  });

  protected readonly newItems = computed<MenuItem[]>(() => [
    { label: 'Create user', icon: 'pi pi-user-plus', visible: this.appConfig.features().users && this.permissionService.can({ any: [Permissions.users.create] }), command: () => this.dispatchOrRoute('create-user', '/users') },
    { label: 'Create role', icon: 'pi pi-id-card', visible: this.appConfig.features().roles && this.permissionService.can({ any: [Permissions.roles.create] }), command: () => this.dispatchOrRoute('create-role', '/roles') },
    { label: 'Create access policy', icon: 'pi pi-shield', visible: this.appConfig.features().accessPolicies && this.permissionService.can({ any: [Permissions.accessPolicies.create] }), command: () => this.dispatchOrRoute('create-access-policy', '/access-policies') },
    { label: 'Create API key', icon: 'pi pi-key', command: () => this.dispatchOrRoute('create-api-key', '/settings') },
    { label: 'Create workspace', icon: 'pi pi-building', command: () => this.dispatchOrRoute('create-workspace', '/settings') }
  ]);

  private readonly commands: Array<{ id: string; label: string; hint: string; icon: string; gate?: PermissionGate }> = [
    { id: 'search-users', label: 'Search users', hint: 'Open the users directory', icon: 'pi pi-search', gate: { any: [Permissions.users.read] } },
    { id: 'create-user', label: 'Create user', hint: 'Open user creation drawer', icon: 'pi pi-user-plus', gate: { any: [Permissions.users.create] } },
    { id: 'create-role', label: 'Create role', hint: 'Open role creation drawer', icon: 'pi pi-id-card', gate: { any: [Permissions.roles.create] } },
    { id: 'create-access-policy', label: 'Create access policy', hint: 'Open policy creation drawer', icon: 'pi pi-shield', gate: { any: [Permissions.accessPolicies.create] } },
    { id: 'open-active-sessions', label: 'Open active sessions', hint: 'Review online users and sessions', icon: 'pi pi-desktop', gate: { any: [Permissions.sessions.read] } },
    { id: 'open-logs', label: 'Open logs', hint: 'Inspect application logs', icon: 'pi pi-list', gate: { any: [Permissions.logs.read] } },
    { id: 'open-error-logs', label: 'Open error logs', hint: 'Inspect latest failures', icon: 'pi pi-exclamation-circle', gate: { any: [Permissions.errorLogs.read] } },
    { id: 'open-settings', label: 'Open settings', hint: 'Configure template options', icon: 'pi pi-cog', gate: { any: [Permissions.settings.read] } },
    { id: 'toggle-dark', label: 'Toggle dark mode', hint: 'Switch light or dark theme', icon: 'pi pi-moon' },
    { id: 'toggle-direction', label: 'Toggle RTL/LTR', hint: 'Switch document direction', icon: 'pi pi-arrows-h' },
    { id: 'switch-workspace', label: 'Switch workspace', hint: 'Placeholder command', icon: 'pi pi-building' }
  ];

  constructor() {
    this.sessionRealtime.start();
  }

  protected filteredCommands() {
    const search = this.commandSearch.trim().toLowerCase();

    return this.commands.filter((command) =>
      this.permissionService.can(command.gate) &&
      (!search || `${command.label} ${command.hint}`.toLowerCase().includes(search))
    );
  }

  protected readonly displayName = computed(
    () => this.authService.session()?.user?.displayName ?? 'Workspace User'
  );

  protected readonly email = computed(
    () => this.authService.session()?.user?.email ?? 'Not signed in'
  );

  protected readonly initials = computed(() => {
    const value = this.displayName();
    return value
      .split(' ')
      .map((part) => part[0])
      .join('')
      .slice(0, 2)
      .toUpperCase();
  });

  protected readonly activeSection = computed(() => {
    const url = this.router.url;

    if (url.startsWith('/users')) {
      return {
        section: 'Identity',
        page: 'Users',
        title: 'Users Administration',
        badge: 'Directory'
      };
    }

    if (url.startsWith('/roles')) {
      return {
        section: 'Identity',
        page: 'Roles',
        title: 'Role Management',
        badge: 'Permissions'
      };
    }

    if (url.startsWith('/access-policies')) {
      return {
        section: 'Security',
        page: 'Policies',
        title: 'Access Policy Controls',
        badge: 'RBAC'
      };
    }

    if (url.startsWith('/active-sessions')) {
      return {
        section: 'Security',
        page: 'Active Sessions',
        title: 'Active Sessions',
        badge: 'Online'
      };
    }

    if (url.startsWith('/logs')) {
      return {
        section: 'Observability',
        page: 'Logs',
        title: 'Application Logs',
        badge: 'Events'
      };
    }

    if (url.startsWith('/error-logs')) {
      return {
        section: 'Observability',
        page: 'Errors',
        title: 'Error Monitoring',
        badge: 'Incidents'
      };
    }

    return {
      section: 'Operations',
      page: 'Overview',
      title: 'Workspace Operations Overview',
      badge: 'Live'
    };
  });

  protected logout(): void {
    this.authFacade.logout().subscribe({
      next: () => {
        this.sessionRealtime.stop();
        this.toast.info('Signed out', 'You have been returned to login.');
        void this.router.navigateByUrl('/auth/login');
      },
      error: () => {
        this.sessionRealtime.stop();
        this.authService.clearSession();
        this.toast.warn('Signed out locally', 'Server logout failed, local session cleared.');
        void this.router.navigateByUrl('/auth/login');
      }
    });
  }

  protected toggleSidebar(): void {
    this.sidebarCollapsed.update((collapsed) => {
      this.appConfig.update({ sidebarCollapsed: !collapsed });
      return !collapsed;
    });
  }

  protected openCommandPalette(): void {
    this.commandSearch = '';
    this.commandPaletteVisible.set(true);
  }

  protected openOperationalConsole(): void {
    this.operationalConsoleVisible.set(true);
  }

  protected openChatConsole(): void {
    this.chatConsoleVisible.set(true);
    void this.loadChatUsers();
  }

  protected clearOperationalConsole(): void {
    this.sessionRealtime.clearOperationalActivity();
  }

  protected onChatDialogVisibilityChange(isVisible: boolean): void {
    if (!isVisible) {
      this.cancelReply();
      this.chatDraft = '';
      const selected = this.selectedChatUserId();
      if (selected) {
        void this.sessionRealtime.setDirectTyping(selected, false);
      }
    }
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

    try {
      const selected = this.selectedChatUserId();
      if (!selected) {
        this.toast.warn('Select user', 'Please select a user first.');
        return;
      }
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

  protected findChatMessageById(messageId: string | null | undefined) {
    if (!messageId) {
      return null;
    }

    return this.activeChatMessages().find((item) => item.id === messageId) ?? null;
  }

  protected async selectChatUser(userId: string): Promise<void> {
    this.selectedChatUserId.set(userId);
    await this.sessionRealtime.loadDirectChatHistory(userId);
  }

  private async loadChatUsers(): Promise<void> {
    if (this.chatUsers().length) {
      return;
    }

    try {
      const page = await firstValueFrom(this.usersApi.getUsers({ PageNumber: 1, PageSize: 100 }));
      const currentUserId = this.authService.session()?.user?.userId;
      const users = page.items.filter((item) => item.id !== currentUserId);
      this.chatUsers.set(users);
      if (users.length && !this.selectedChatUserId()) {
        await this.selectChatUser(users[0].id);
      }
    } catch {
      this.toast.error('Users load failed', 'Unable to load users for chat.');
    }
  }

  protected formatActivityTime(value: string): string {
    return new Date(value).toLocaleTimeString([], {
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit'
    });
  }

  protected activityLevelClass(level: string): string {
    switch (level.toLowerCase()) {
      case 'error':
        return 'font-semibold text-rose-300';
      case 'warn':
        return 'font-semibold text-amber-300';
      default:
        return 'font-semibold text-emerald-300';
    }
  }

  protected runCommand(id: string): void {
    this.commandPaletteVisible.set(false);

    switch (id) {
      case 'search-users':
        void this.router.navigateByUrl('/users');
        break;
      case 'create-user':
        this.dispatchOrRoute('create-user', '/users');
        break;
      case 'create-role':
        this.dispatchOrRoute('create-role', '/roles');
        break;
      case 'create-access-policy':
        this.dispatchOrRoute('create-access-policy', '/access-policies');
        break;
      case 'open-logs':
        void this.router.navigateByUrl('/logs');
        break;
      case 'open-active-sessions':
        void this.router.navigateByUrl('/active-sessions');
        break;
      case 'open-error-logs':
        void this.router.navigateByUrl('/error-logs');
        break;
      case 'open-settings':
        void this.router.navigateByUrl('/settings');
        break;
      case 'toggle-dark':
        this.theme.toggle();
        break;
      case 'toggle-direction':
        this.appConfig.toggleDirection();
        break;
      default:
        this.toast.info('Workspace switcher', 'Placeholder for multi-workspace templates.');
        break;
    }
  }

  @HostListener('window:keydown', ['$event'])
  protected handleShortcut(event: KeyboardEvent): void {
    if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'k') {
      event.preventDefault();
      this.openCommandPalette();
    }
  }

  private dispatchOrRoute(action: AdminGlobalAction, route: string): void {
    if (this.router.url.startsWith(route)) {
      this.actionBus.dispatch(action);
      return;
    }

    void this.router.navigateByUrl(route).then(() => this.actionBus.dispatch(action));
  }
}

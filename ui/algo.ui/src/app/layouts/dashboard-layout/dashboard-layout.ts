import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { DividerModule } from 'primeng/divider';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { InputTextModule } from 'primeng/inputtext';
import { TagModule } from 'primeng/tag';
import { TooltipModule } from 'primeng/tooltip';
import { AppToastService } from '../../core/services/app-toast.service';
import { AuthService } from '../../core/services/auth.service';
import { ThemeService } from '../../core/services/theme.service';

@Component({
  selector: 'app-dashboard-layout',
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    ButtonModule,
    DividerModule,
    IconFieldModule,
    InputIconModule,
    InputTextModule,
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
                  <div class="eyebrow">algo.ui workspace</div>
                  <div class="truncate text-sm font-semibold text-surface-950">Admin Console</div>
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
                <span class="text-[10px] text-slate-400">6 modules</span>
              </div>
            }

            <nav class="flex flex-col gap-1">
              @for (item of navigation; track item.path) {
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

                  <div class="flex items-center gap-1.5">
                    <p-button
                      icon="pi pi-bolt"
                      label="Quick action"
                      severity="secondary"
                      size="small"
                      [outlined]="true"
                    />
                    <p-button icon="pi pi-plus" label="New" size="small" />
                  </div>
                </div>
              </div>
            </header>

            <router-outlet />
          </div>
        </section>
      </div>
    </main>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DashboardLayout {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly toast = inject(AppToastService);
  protected readonly theme = inject(ThemeService);

  protected readonly sidebarCollapsed = signal(false);

  protected readonly navigation = [
    { label: 'Overview', path: '/dashboard', icon: 'pi pi-home' },
    { label: 'Users', path: '/users', icon: 'pi pi-users', badge: 'IAM' },
    { label: 'Roles', path: '/roles', icon: 'pi pi-id-card' },
    { label: 'Access Policies', path: '/access-policies', icon: 'pi pi-shield', badge: 'Auth' },
    { label: 'Logs', path: '/logs', icon: 'pi pi-list' },
    { label: 'Error Logs', path: '/error-logs', icon: 'pi pi-exclamation-circle', badge: 'Ops' }
  ];

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
    this.authService.clearSession();
    this.toast.info('Signed out', 'You have been returned to login.');
    void this.router.navigateByUrl('/auth/login');
  }

  protected toggleSidebar(): void {
    this.sidebarCollapsed.update((collapsed) => !collapsed);
  }
}

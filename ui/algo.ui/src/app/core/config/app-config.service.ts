import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import { Injectable, PLATFORM_ID, computed, effect, inject, signal } from '@angular/core';

import {
  AdminDirection,
  AdminTemplateConfig,
  DEFAULT_ADMIN_TEMPLATE_CONFIG
} from './admin-template-config.model';

const CONFIG_KEY = 'algo.ui.admin-template.config';

@Injectable({ providedIn: 'root' })
export class AppConfigService {
  private readonly document = inject(DOCUMENT);
  private readonly platformId = inject(PLATFORM_ID);
  private readonly configState = signal<AdminTemplateConfig>(this.readConfig());

  readonly config = this.configState.asReadonly();
  readonly features = computed(() => this.configState().features);
  readonly isDark = computed(() => this.configState().theme === 'dark');
  readonly isCompact = computed(() => this.configState().compactMode);
  readonly direction = computed(() => this.configState().direction);
  readonly apiBaseUrl = computed(() => this.configState().apiBaseUrl);

  constructor() {
    effect(() => {
      const config = this.configState();
      this.applyDocumentState(config);

      if (isPlatformBrowser(this.platformId)) {
        localStorage.setItem(CONFIG_KEY, JSON.stringify(config));
      }
    });
  }

  update(patch: Partial<AdminTemplateConfig>): void {
    this.configState.update((current) => ({
      ...current,
      ...patch,
      passwordPolicy: {
        ...current.passwordPolicy,
        ...patch.passwordPolicy
      },
      features: {
        ...current.features,
        ...patch.features
      }
    }));
  }

  setDirection(direction: AdminDirection): void {
    this.update({ direction });
  }

  toggleDirection(): void {
    this.setDirection(this.direction() === 'rtl' ? 'ltr' : 'rtl');
  }

  toggleTheme(): void {
    this.update({ theme: this.isDark() ? 'light' : 'dark' });
  }

  toggleCompactMode(): void {
    this.update({ compactMode: !this.isCompact() });
  }

  reset(): void {
    this.configState.set(DEFAULT_ADMIN_TEMPLATE_CONFIG);
  }

  private readConfig(): AdminTemplateConfig {
    if (!isPlatformBrowser(this.platformId)) {
      return DEFAULT_ADMIN_TEMPLATE_CONFIG;
    }

    const raw = localStorage.getItem(CONFIG_KEY);
    if (!raw) {
      return DEFAULT_ADMIN_TEMPLATE_CONFIG;
    }

    try {
      const parsed = JSON.parse(raw) as Partial<AdminTemplateConfig>;

      return {
        ...DEFAULT_ADMIN_TEMPLATE_CONFIG,
        ...parsed,
        passwordPolicy: {
          ...DEFAULT_ADMIN_TEMPLATE_CONFIG.passwordPolicy,
          ...parsed.passwordPolicy
        },
        features: {
          ...DEFAULT_ADMIN_TEMPLATE_CONFIG.features,
          ...parsed.features
        }
      };
    } catch {
      return DEFAULT_ADMIN_TEMPLATE_CONFIG;
    }
  }

  private applyDocumentState(config: AdminTemplateConfig): void {
    const root = this.document.documentElement;
    const body = this.document.body;
    const dark = config.theme === 'dark';

    root.dir = config.direction;
    root.lang = config.defaultLanguage;
    root.style.setProperty('--accent', config.primaryColor);
    root.classList.toggle('p-dark', dark);
    root.classList.toggle('app-dark', dark);
    root.classList.toggle('app-compact', config.compactMode);
    root.classList.toggle('app-sharp', config.shape === 'sharp');
    body?.classList.toggle('p-dark', dark);
    body?.classList.toggle('app-dark', dark);
    body?.classList.toggle('app-compact', config.compactMode);
    body?.classList.toggle('app-sharp', config.shape === 'sharp');

    if (config.faviconUrl) {
      this.setFavicon(config.faviconUrl);
    }
  }

  private setFavicon(url: string): void {
    let link = this.document.querySelector<HTMLLinkElement>('link[rel="icon"]');
    if (!link) {
      link = this.document.createElement('link');
      link.rel = 'icon';
      this.document.head.appendChild(link);
    }
    link.href = url;
  }
}

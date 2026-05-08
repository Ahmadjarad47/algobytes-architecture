import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import { Injectable, PLATFORM_ID, computed, effect, inject, signal } from '@angular/core';

type ThemeMode = 'light' | 'dark';

const THEME_KEY = 'algo.ui.theme';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly document = inject(DOCUMENT);
  private readonly platformId = inject(PLATFORM_ID);
  private readonly modeState = signal<ThemeMode>(this.readMode());

  readonly mode = this.modeState.asReadonly();
  readonly isDark = computed(() => this.modeState() === 'dark');

  constructor() {
    effect(() => {
      this.applyMode(this.modeState());
    });
  }

  toggle(): void {
    this.modeState.update((mode) => (mode === 'dark' ? 'light' : 'dark'));
  }

  private readMode(): ThemeMode {
    if (!isPlatformBrowser(this.platformId)) {
      return 'light';
    }

    const storedMode = localStorage.getItem(THEME_KEY);
    if (storedMode === 'light' || storedMode === 'dark') {
      return storedMode;
    }

    return window.matchMedia?.('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
  }

  private applyMode(mode: ThemeMode): void {
    const root = this.document.documentElement;
    const body = this.document.body;

    root.classList.toggle('p-dark', mode === 'dark');
    root.classList.toggle('app-dark', mode === 'dark');
    body?.classList.toggle('p-dark', mode === 'dark');
    body?.classList.toggle('app-dark', mode === 'dark');

    if (isPlatformBrowser(this.platformId)) {
      localStorage.setItem(THEME_KEY, mode);
    }
  }
}

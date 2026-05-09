import { Injectable, computed, inject } from '@angular/core';

import { AppConfigService } from '../config/app-config.service';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly appConfig = inject(AppConfigService);

  readonly mode = computed(() => this.appConfig.config().theme);
  readonly isDark = this.appConfig.isDark;

  toggle(): void {
    this.appConfig.toggleTheme();
  }
}

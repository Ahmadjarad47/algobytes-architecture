import { Routes } from '@angular/router';

export const SETTINGS_ROUTES: Routes = [
  {
    path: '',
    title: 'Settings | algo.ui',
    loadComponent: () =>
      import('./pages/settings-home/settings-home').then(
        (m) => m.SettingsHome
      )
  }
];

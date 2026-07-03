import { Routes } from '@angular/router';

export const LANDING_ROUTES: Routes = [
  {
    path: '',
    title: 'المهندس | اشحن ألعابك المفضلة',
    loadComponent: () =>
      import('./pages/landing-page/landing-page').then((m) => m.LandingPage)
  }
];

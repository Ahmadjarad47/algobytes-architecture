import { Routes } from '@angular/router';

export const ACCESS_POLICIES_ROUTES: Routes = [
  {
    path: '',
    title: 'Access Policies | algo.ui',
    loadComponent: () =>
      import('./pages/access-policies-list/access-policies-list').then(
        (m) => m.AccessPoliciesList
      )
  }
];

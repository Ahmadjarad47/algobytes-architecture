import { Routes } from '@angular/router';

export const CATEGORIES_ROUTES: Routes = [
  {
    path: '',
    title: 'Categories | algo.ui',
    loadComponent: () =>
      import('./pages/categories-list/categories-list').then((m) => m.CategoriesList)
  }
];

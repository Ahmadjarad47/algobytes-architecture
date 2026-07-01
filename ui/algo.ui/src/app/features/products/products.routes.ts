import { Routes } from '@angular/router';

export const PRODUCTS_ROUTES: Routes = [
  {
    path: '',
    title: 'Products | algo.ui',
    loadComponent: () =>
      import('./pages/products-list/products-list').then((m) => m.ProductsList)
  }
];

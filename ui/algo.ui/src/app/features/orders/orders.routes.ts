import { Routes } from '@angular/router';

export const ORDERS_ROUTES: Routes = [
  {
    path: '',
    title: 'Orders | algo.ui',
    loadComponent: () =>
      import('./pages/orders-list/orders-list').then((m) => m.OrdersList)
  }
];

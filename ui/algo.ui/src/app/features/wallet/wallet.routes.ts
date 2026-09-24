import { Routes } from '@angular/router';

export const WALLET_ROUTES: Routes = [
  {
    path: '',
    title: 'Wallet | algo.ui',
    loadComponent: () =>
      import('./pages/wallet-home/wallet-home').then((m) => m.WalletHome)
  }
];

export const ADMIN_WALLET_ROUTES: Routes = [
  {
    path: '',
    title: 'Admin Wallets | algo.ui',
    loadComponent: () =>
      import('./pages/admin-wallet-dashboard/admin-wallet-dashboard').then((m) => m.AdminWalletDashboard)
  }
];

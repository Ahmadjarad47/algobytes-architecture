import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiService } from '../../../core/api/api.service';
import {
  AdminWalletOverviewDto,
  ChargeWalletCommand,
  StopWalletRequest,
  WalletBalanceDto,
  WalletFundsCommand,
  WalletTransactionDto
} from '../models/wallet.models';

@Injectable({ providedIn: 'root' })
export class WalletApiService {
  private readonly api = inject(ApiService);

  getBalance(): Observable<WalletBalanceDto[]> {
    return this.api.get<WalletBalanceDto[]>('/Wallet/balance');
  }

  charge(command: ChargeWalletCommand): Observable<WalletTransactionDto> {
    return this.api.post<WalletTransactionDto, ChargeWalletCommand>('/Wallet/charge', command);
  }

  withdraw(command: WalletFundsCommand): Observable<WalletTransactionDto> {
    return this.api.post<WalletTransactionDto, WalletFundsCommand>('/Wallet/withdraw', command);
  }

  freeze(command: WalletFundsCommand): Observable<WalletTransactionDto> {
    return this.api.post<WalletTransactionDto, WalletFundsCommand>('/Wallet/freeze', command);
  }

  unfreeze(command: WalletFundsCommand): Observable<WalletTransactionDto> {
    return this.api.post<WalletTransactionDto, WalletFundsCommand>('/Wallet/unfreeze', command);
  }

  getTransactions(): Observable<WalletTransactionDto[]> {
    return this.api.get<WalletTransactionDto[]>('/Wallet/transactions');
  }

  getAdminOverview(): Observable<AdminWalletOverviewDto> {
    return this.api.get<AdminWalletOverviewDto>('/Wallet/admin/overview');
  }

  stopUserWallet(userId: string, request: StopWalletRequest): Observable<WalletTransactionDto[]> {
    return this.api.patch<WalletTransactionDto[], StopWalletRequest>(`/Wallet/admin/users/${userId}/stop`, request);
  }

  deleteUserWalletTransactions(userId: string): Observable<number> {
    return this.api.delete<number>(`/Wallet/admin/users/${userId}/transactions`);
  }
}

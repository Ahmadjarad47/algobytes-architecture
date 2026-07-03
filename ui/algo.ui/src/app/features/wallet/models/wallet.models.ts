export interface WalletBalanceDto {
  readonly currencyCode: string;
  readonly balance: number;
}

export interface WalletTransactionDto {
  readonly id: number;
  readonly userId: string;
  readonly currencyCode: string;
  readonly amount: number;
  readonly transactionType: string;
  readonly description: string | null;
  readonly referenceId: string | null;
  readonly createdAt: string;
}

export interface ChargeWalletCommand {
  readonly currencyCode: string;
  readonly amount: number;
  readonly description?: string | null;
}

export interface WalletFundsCommand {
  readonly currencyCode: string;
  readonly amount: number;
  readonly description?: string | null;
}

export interface AdminWalletOverviewDto {
  readonly currencySummaries: AdminWalletCurrencySummaryDto[];
  readonly wallets: AdminWalletUserDto[];
  readonly transactions: AdminWalletTransactionDto[];
  readonly dailyMovements: AdminWalletDailyMovementDto[];
}

export interface AdminWalletCurrencySummaryDto {
  readonly currencyCode: string;
  readonly totalBalance: number;
  readonly totalDeposits: number;
  readonly totalWithdrawals: number;
  readonly totalFrozen: number;
  readonly walletCount: number;
  readonly transactionCount: number;
}

export interface AdminWalletUserDto {
  readonly userId: string;
  readonly email: string | null;
  readonly userName: string | null;
  readonly displayName: string;
  readonly isActive: boolean;
  readonly totalBalance: number;
  readonly totalDeposits: number;
  readonly totalWithdrawals: number;
  readonly totalFrozen: number;
  readonly transactionCount: number;
  readonly lastTransactionAt: string | null;
  readonly balances: AdminWalletBalanceDto[];
}

export interface AdminWalletBalanceDto {
  readonly currencyCode: string;
  readonly balance: number;
  readonly deposits: number;
  readonly withdrawals: number;
  readonly frozen: number;
}

export interface AdminWalletTransactionDto {
  readonly id: number;
  readonly userId: string;
  readonly email: string | null;
  readonly displayName: string;
  readonly currencyCode: string;
  readonly amount: number;
  readonly transactionType: string;
  readonly description: string | null;
  readonly referenceId: string | null;
  readonly createdAt: string;
}

export interface AdminWalletDailyMovementDto {
  readonly date: string;
  readonly currencyCode: string;
  readonly deposits: number;
  readonly withdrawals: number;
  readonly netMovement: number;
}

export interface StopWalletRequest {
  readonly description?: string | null;
}

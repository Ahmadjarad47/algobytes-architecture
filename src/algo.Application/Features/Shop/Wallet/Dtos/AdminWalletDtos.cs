namespace algo.Application.Features.Shop.Wallet.Dtos;

public sealed record AdminWalletOverviewDto(
    IReadOnlyList<AdminWalletCurrencySummaryDto> CurrencySummaries,
    IReadOnlyList<AdminWalletUserDto> Wallets,
    IReadOnlyList<AdminWalletTransactionDto> Transactions,
    IReadOnlyList<AdminWalletDailyMovementDto> DailyMovements);

public sealed record AdminWalletCurrencySummaryDto(
    string CurrencyCode,
    decimal TotalBalance,
    decimal TotalDeposits,
    decimal TotalWithdrawals,
    decimal TotalFrozen,
    int WalletCount,
    int TransactionCount);

public sealed record AdminWalletUserDto(
    string UserId,
    string? Email,
    string? UserName,
    string DisplayName,
    bool IsActive,
    decimal TotalBalance,
    decimal TotalDeposits,
    decimal TotalWithdrawals,
    decimal TotalFrozen,
    int TransactionCount,
    DateTimeOffset? LastTransactionAt,
    IReadOnlyList<AdminWalletBalanceDto> Balances);

public sealed record AdminWalletBalanceDto(
    string CurrencyCode,
    decimal Balance,
    decimal Deposits,
    decimal Withdrawals,
    decimal Frozen);

public sealed record AdminWalletTransactionDto(
    long Id,
    string UserId,
    string? Email,
    string DisplayName,
    string CurrencyCode,
    decimal Amount,
    string TransactionType,
    string? Description,
    string? ReferenceId,
    DateTimeOffset CreatedAt);

public sealed record AdminWalletDailyMovementDto(
    DateOnly Date,
    string CurrencyCode,
    decimal Deposits,
    decimal Withdrawals,
    decimal NetMovement);

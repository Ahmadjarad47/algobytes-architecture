using algo.Application.Common.AccessPolicy;
using algo.Application.Features.Shop.Wallet.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace algo.Application.Features.Shop.Wallet.Queries.GetAdminWalletOverview;

public sealed class GetAdminWalletOverviewQueryHandler(
    IApplicationDbContext db,
    IAccessPolicyAuthorizationChecker authorizationChecker)
    : IRequestHandler<GetAdminWalletOverviewQuery, AdminWalletOverviewDto>
{
    public async Task<AdminWalletOverviewDto> Handle(GetAdminWalletOverviewQuery request, CancellationToken cancellationToken)
    {
        await authorizationChecker.EnsureResourceActionAllowedAsync(
            db,
            AccessPolicyResources.Wallet,
            AccessPolicyActions.Read,
            cancellationToken);

        var users = await db.Users
            .AsNoTracking()
            .Where(user => user.DeletedAt == null)
            .Select(user => new
            {
                user.Id,
                user.Email,
                user.UserName,
                user.DisplayName,
                user.IsActive,
            })
            .ToListAsync(cancellationToken);

        var transactions = await db.WalletTransactions
            .AsNoTracking()
            .OrderByDescending(transaction => transaction.CreatedAt)
            .Select(transaction => new WalletTransactionSnapshot(
                transaction.Id,
                transaction.UserId,
                transaction.CurrencyCode,
                transaction.Amount,
                transaction.TransactionType,
                transaction.Description,
                transaction.ReferenceId,
                transaction.CreatedAt))
            .ToListAsync(cancellationToken);

        var usersById = users.ToDictionary(user => user.Id, StringComparer.Ordinal);
        var walletUserIds = transactions.Select(transaction => transaction.UserId).ToHashSet(StringComparer.Ordinal);

        var wallets = users
            .Where(user => walletUserIds.Contains(user.Id))
            .Select(user =>
            {
                var userTransactions = transactions
                    .Where(transaction => string.Equals(transaction.UserId, user.Id, StringComparison.Ordinal))
                    .ToList();

                var balances = userTransactions
                    .GroupBy(transaction => transaction.CurrencyCode, StringComparer.OrdinalIgnoreCase)
                    .Select(group => new AdminWalletBalanceDto(
                        group.Key,
                        group.Sum(transaction => transaction.Amount),
                        group.Where(IsDeposit).Sum(transaction => transaction.Amount),
                        group.Where(IsWithdrawal).Sum(transaction => Math.Abs(transaction.Amount)),
                        group.Where(IsFrozen).Sum(transaction => Math.Abs(transaction.Amount))))
                    .OrderBy(balance => balance.CurrencyCode)
                    .ToList();

                return new AdminWalletUserDto(
                    user.Id,
                    user.Email,
                    user.UserName,
                    user.DisplayName,
                    user.IsActive,
                    balances.Sum(balance => balance.Balance),
                    balances.Sum(balance => balance.Deposits),
                    balances.Sum(balance => balance.Withdrawals),
                    balances.Sum(balance => balance.Frozen),
                    userTransactions.Count,
                    userTransactions.MaxBy(transaction => transaction.CreatedAt)?.CreatedAt,
                    balances);
            })
            .OrderByDescending(wallet => wallet.TotalBalance)
            .ThenBy(wallet => wallet.Email)
            .ToList();

        var currencySummaries = transactions
            .GroupBy(transaction => transaction.CurrencyCode, StringComparer.OrdinalIgnoreCase)
            .Select(group => new AdminWalletCurrencySummaryDto(
                group.Key,
                group.Sum(transaction => transaction.Amount),
                group.Where(IsDeposit).Sum(transaction => transaction.Amount),
                group.Where(IsWithdrawal).Sum(transaction => Math.Abs(transaction.Amount)),
                group.Where(IsFrozen).Sum(transaction => Math.Abs(transaction.Amount)),
                group.Select(transaction => transaction.UserId).Distinct(StringComparer.Ordinal).Count(),
                group.Count()))
            .OrderBy(summary => summary.CurrencyCode)
            .ToList();

        var dailyMovements = transactions
            .GroupBy(transaction => new { Date = DateOnly.FromDateTime(transaction.CreatedAt.UtcDateTime.Date), transaction.CurrencyCode })
            .Select(group => new AdminWalletDailyMovementDto(
                group.Key.Date,
                group.Key.CurrencyCode,
                group.Where(IsDeposit).Sum(transaction => transaction.Amount),
                group.Where(IsWithdrawal).Sum(transaction => Math.Abs(transaction.Amount)),
                group.Sum(transaction => transaction.Amount)))
            .OrderBy(movement => movement.Date)
            .ThenBy(movement => movement.CurrencyCode)
            .ToList();

        var adminTransactions = transactions
            .Select(transaction =>
            {
                usersById.TryGetValue(transaction.UserId, out var user);
                return new AdminWalletTransactionDto(
                    transaction.Id,
                    transaction.UserId,
                    user?.Email,
                    user?.DisplayName ?? "Deleted user",
                    transaction.CurrencyCode,
                    transaction.Amount,
                    transaction.TransactionType,
                    transaction.Description,
                    transaction.ReferenceId,
                    transaction.CreatedAt);
            })
            .ToList();

        return new AdminWalletOverviewDto(currencySummaries, wallets, adminTransactions, dailyMovements);
    }

    private static bool IsDeposit(WalletTransactionSnapshot transaction) =>
        transaction.Amount > 0 && !string.Equals(transaction.TransactionType, "Unfreeze", StringComparison.OrdinalIgnoreCase);

    private static bool IsWithdrawal(WalletTransactionSnapshot transaction) =>
        transaction.Amount < 0 && !string.Equals(transaction.TransactionType, "Freeze", StringComparison.OrdinalIgnoreCase);

    private static bool IsFrozen(WalletTransactionSnapshot transaction) =>
        string.Equals(transaction.TransactionType, "Freeze", StringComparison.OrdinalIgnoreCase);

    private sealed record WalletTransactionSnapshot(
        long Id,
        string UserId,
        string CurrencyCode,
        decimal Amount,
        string TransactionType,
        string? Description,
        string? ReferenceId,
        DateTimeOffset CreatedAt);
}

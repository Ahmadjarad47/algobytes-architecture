using algo.Application.Common.AccessPolicy;
using algo.Application.Features.Shop.Wallet.Dtos;
using algo.Domain.Sales.Entities;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace algo.Application.Features.Shop.Wallet.Commands.StopUserWallet;

public sealed class StopUserWalletCommandHandler(
    IApplicationDbContext db,
    IAccessPolicyAuthorizationChecker authorizationChecker)
    : IRequestHandler<StopUserWalletCommand, IReadOnlyList<WalletTransactionDto>>
{
    public async Task<IReadOnlyList<WalletTransactionDto>> Handle(StopUserWalletCommand request, CancellationToken cancellationToken)
    {
        await authorizationChecker.EnsureResourceActionAllowedAsync(
            db,
            AccessPolicyResources.Wallet,
            AccessPolicyActions.Update,
            cancellationToken);

        var userExists = await db.Users
            .AnyAsync(user => user.Id == request.UserId && user.DeletedAt == null, cancellationToken);

        if (!userExists)
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(request.UserId), "User was not found."),
            });
        }

        var positiveBalances = await db.WalletTransactions
            .Where(transaction => transaction.UserId == request.UserId)
            .GroupBy(transaction => transaction.CurrencyCode)
            .Select(group => new
            {
                CurrencyCode = group.Key,
                Balance = group.Sum(transaction => transaction.Amount),
            })
            .Where(balance => balance.Balance > 0)
            .ToListAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var description = string.IsNullOrWhiteSpace(request.Description)
            ? "Admin stopped wallet by freezing all positive balances"
            : request.Description.Trim();

        var transactions = positiveBalances
            .Select(balance => new WalletTransaction
            {
                UserId = request.UserId,
                CurrencyCode = balance.CurrencyCode,
                Amount = -balance.Balance,
                TransactionType = "Freeze",
                Description = description,
                ReferenceId = $"admin-stop:{now:yyyyMMddHHmmss}",
                CreatedAt = now,
            })
            .ToList();

        if (transactions.Count == 0)
        {
            return [];
        }

        db.WalletTransactions.AddRange(transactions);
        await db.SaveChangesAsync(cancellationToken);

        return transactions
            .Select(transaction => new WalletTransactionDto(
                transaction.Id,
                transaction.UserId,
                transaction.CurrencyCode,
                transaction.Amount,
                transaction.TransactionType,
                transaction.Description,
                transaction.ReferenceId,
                transaction.CreatedAt))
            .ToList();
    }
}

using algo.Application.Abstractions;
using algo.Application.Features.Shop.Wallet.Dtos;
using algo.Domain.Sales.Entities;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace algo.Application.Features.Shop.Wallet.Commands.UnfreezeWalletFunds;

public sealed class UnfreezeWalletFundsCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<UnfreezeWalletFundsCommand, WalletTransactionDto>
{
    public async Task<WalletTransactionDto> Handle(UnfreezeWalletFundsCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(currentUser.UserId))
        {
            throw new UnauthorizedAccessException("Current user is not available.");
        }

        if (request.Amount <= 0)
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(request.Amount), "Amount must be greater than zero."),
            });
        }

        var currencyCode = request.CurrencyCode.Trim().ToUpperInvariant();
        var frozenAmount = await db.WalletTransactions
            .Where(walletTransaction =>
                walletTransaction.UserId == currentUser.UserId &&
                walletTransaction.CurrencyCode == currencyCode &&
                (walletTransaction.TransactionType == "Freeze" || walletTransaction.TransactionType == "Unfreeze"))
            .SumAsync(
                walletTransaction => walletTransaction.TransactionType == "Freeze"
                    ? -walletTransaction.Amount
                    : -walletTransaction.Amount,
                cancellationToken);

        if (frozenAmount < request.Amount)
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(request.Amount), "Insufficient frozen wallet funds."),
            });
        }

        var transaction = new WalletTransaction
        {
            UserId = currentUser.UserId!,
            CurrencyCode = currencyCode,
            Amount = request.Amount,
            TransactionType = "Unfreeze",
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
        };

        db.WalletTransactions.Add(transaction);
        await db.SaveChangesAsync(cancellationToken);

        return new WalletTransactionDto(
            transaction.Id,
            transaction.UserId,
            transaction.CurrencyCode,
            transaction.Amount,
            transaction.TransactionType,
            transaction.Description,
            transaction.ReferenceId,
            transaction.CreatedAt);
    }
}

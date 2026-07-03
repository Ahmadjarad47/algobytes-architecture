using algo.Application.Abstractions;
using algo.Application.Features.Shop.Wallet.Dtos;
using algo.Domain.Sales.Entities;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace algo.Application.Features.Shop.Wallet.Commands.FreezeWalletFunds;

public sealed class FreezeWalletFundsCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<FreezeWalletFundsCommand, WalletTransactionDto>
{
    public async Task<WalletTransactionDto> Handle(FreezeWalletFundsCommand request, CancellationToken cancellationToken)
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
        var balance = await db.WalletTransactions
            .Where(walletTransaction => walletTransaction.UserId == currentUser.UserId && walletTransaction.CurrencyCode == currencyCode)
            .SumAsync(walletTransaction => walletTransaction.Amount, cancellationToken);

        if (balance < request.Amount)
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(request.Amount), "Insufficient wallet balance."),
            });
        }

        var transaction = new WalletTransaction
        {
            UserId = currentUser.UserId!,
            CurrencyCode = currencyCode,
            Amount = -request.Amount,
            TransactionType = "Freeze",
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

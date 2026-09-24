using algo.Application.Abstractions;
using algo.Application.Features.Shop.Wallet.Dtos;
using algo.Domain.Sales.Entities;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace algo.Application.Features.Shop.Wallet.Commands.ChargeWallet;

public sealed class ChargeWalletCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<ChargeWalletCommand, WalletTransactionDto>
{
    public async Task<WalletTransactionDto> Handle(ChargeWalletCommand request, CancellationToken cancellationToken)
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

        var transaction = new WalletTransaction
        {
            UserId = currentUser.UserId!,
            CurrencyCode = request.CurrencyCode.Trim().ToUpperInvariant(),
            Amount = request.Amount,
            TransactionType = "Charge",
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

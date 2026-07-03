using algo.Application.Abstractions;
using algo.Application.Features.Shop.Wallet.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace algo.Application.Features.Shop.Wallet.Queries.GetWalletTransactions;

public sealed class GetWalletTransactionsQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<GetWalletTransactionsQuery, IReadOnlyList<WalletTransactionDto>>
{
    public async Task<IReadOnlyList<WalletTransactionDto>> Handle(GetWalletTransactionsQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(currentUser.UserId))
        {
            throw new UnauthorizedAccessException("Current user is not available.");
        }

        return await db.WalletTransactions
            .AsNoTracking()
            .Where(walletTransaction => walletTransaction.UserId == currentUser.UserId)
            .OrderByDescending(walletTransaction => walletTransaction.CreatedAt)
            .Select(walletTransaction => new WalletTransactionDto(
                walletTransaction.Id,
                walletTransaction.UserId,
                walletTransaction.CurrencyCode,
                walletTransaction.Amount,
                walletTransaction.TransactionType,
                walletTransaction.Description,
                walletTransaction.ReferenceId,
                walletTransaction.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}

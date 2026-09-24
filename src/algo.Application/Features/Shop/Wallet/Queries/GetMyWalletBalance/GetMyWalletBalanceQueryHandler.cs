using algo.Application.Abstractions;
using algo.Application.Features.Shop.Wallet.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace algo.Application.Features.Shop.Wallet.Queries.GetMyWalletBalance;

public sealed class GetMyWalletBalanceQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<GetMyWalletBalanceQuery, IReadOnlyList<WalletBalanceDto>>
{
    public async Task<IReadOnlyList<WalletBalanceDto>> Handle(GetMyWalletBalanceQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(currentUser.UserId))
        {
            throw new UnauthorizedAccessException("Current user is not available.");
        }

        return await db.WalletTransactions
            .AsNoTracking()
            .Where(walletTransaction => walletTransaction.UserId == currentUser.UserId)
            .GroupBy(walletTransaction => walletTransaction.CurrencyCode)
            .Select(group => new WalletBalanceDto(
                group.Key,
                group.Sum(walletTransaction => walletTransaction.Amount)))
            .ToListAsync(cancellationToken);
    }
}

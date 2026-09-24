using algo.Application.Common.AccessPolicy;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace algo.Application.Features.Shop.Wallet.Commands.DeleteUserWalletTransactions;

public sealed class DeleteUserWalletTransactionsCommandHandler(
    IApplicationDbContext db,
    IAccessPolicyAuthorizationChecker authorizationChecker)
    : IRequestHandler<DeleteUserWalletTransactionsCommand, int>
{
    public async Task<int> Handle(DeleteUserWalletTransactionsCommand request, CancellationToken cancellationToken)
    {
        await authorizationChecker.EnsureResourceActionAllowedAsync(
            db,
            AccessPolicyResources.Wallet,
            AccessPolicyActions.Delete,
            cancellationToken);

        var userExists = await db.Users
            .AnyAsync(user => user.Id == request.UserId, cancellationToken);

        if (!userExists)
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(request.UserId), "User was not found."),
            });
        }

        var transactions = await db.WalletTransactions
            .Where(transaction => transaction.UserId == request.UserId)
            .ToListAsync(cancellationToken);

        if (transactions.Count == 0)
        {
            return 0;
        }

        db.WalletTransactions.RemoveRange(transactions);
        await db.SaveChangesAsync(cancellationToken);

        return transactions.Count;
    }
}

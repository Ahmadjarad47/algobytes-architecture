using MediatR;

namespace algo.Application.Features.Shop.Wallet.Commands.DeleteUserWalletTransactions;

public sealed record DeleteUserWalletTransactionsCommand(string UserId) : IRequest<int>;

using algo.Application.Features.Shop.Wallet.Dtos;
using MediatR;

namespace algo.Application.Features.Shop.Wallet.Queries.GetWalletTransactions;

public sealed record GetWalletTransactionsQuery : IRequest<IReadOnlyList<WalletTransactionDto>>;

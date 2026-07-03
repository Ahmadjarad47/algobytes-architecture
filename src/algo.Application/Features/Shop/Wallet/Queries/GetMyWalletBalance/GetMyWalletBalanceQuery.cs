using algo.Application.Features.Shop.Wallet.Dtos;
using MediatR;

namespace algo.Application.Features.Shop.Wallet.Queries.GetMyWalletBalance;

public sealed record GetMyWalletBalanceQuery : IRequest<IReadOnlyList<WalletBalanceDto>>;

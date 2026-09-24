using algo.Application.Features.Shop.Wallet.Dtos;
using MediatR;

namespace algo.Application.Features.Shop.Wallet.Commands.StopUserWallet;

public sealed record StopUserWalletCommand(
    string UserId,
    string? Description) : IRequest<IReadOnlyList<WalletTransactionDto>>;

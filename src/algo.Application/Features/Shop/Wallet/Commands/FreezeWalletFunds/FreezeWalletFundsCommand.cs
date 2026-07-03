using algo.Application.Features.Shop.Wallet.Dtos;
using MediatR;

namespace algo.Application.Features.Shop.Wallet.Commands.FreezeWalletFunds;

public sealed record FreezeWalletFundsCommand(
    string CurrencyCode,
    decimal Amount,
    string? Description) : IRequest<WalletTransactionDto>;

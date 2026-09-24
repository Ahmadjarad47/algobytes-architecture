using algo.Application.Features.Shop.Wallet.Dtos;
using MediatR;

namespace algo.Application.Features.Shop.Wallet.Commands.UnfreezeWalletFunds;

public sealed record UnfreezeWalletFundsCommand(
    string CurrencyCode,
    decimal Amount,
    string? Description) : IRequest<WalletTransactionDto>;

using algo.Application.Features.Shop.Wallet.Dtos;
using MediatR;

namespace algo.Application.Features.Shop.Wallet.Commands.WithdrawWalletFunds;

public sealed record WithdrawWalletFundsCommand(
    string CurrencyCode,
    decimal Amount,
    string? Description) : IRequest<WalletTransactionDto>;

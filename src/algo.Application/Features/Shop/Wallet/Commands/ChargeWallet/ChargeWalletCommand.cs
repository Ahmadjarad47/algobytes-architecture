using algo.Application.Features.Shop.Wallet.Dtos;
using MediatR;

namespace algo.Application.Features.Shop.Wallet.Commands.ChargeWallet;

public sealed record ChargeWalletCommand(
    string CurrencyCode,
    decimal Amount,
    string? Description) : IRequest<WalletTransactionDto>;

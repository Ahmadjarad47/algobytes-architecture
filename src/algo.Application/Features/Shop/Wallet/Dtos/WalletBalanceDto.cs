namespace algo.Application.Features.Shop.Wallet.Dtos;

public sealed record WalletBalanceDto(
    string CurrencyCode,
    decimal Balance);

namespace algo.Application.Features.Shop.Wallet.Dtos;

public sealed record WalletTransactionDto(
    long Id,
    string UserId,
    string CurrencyCode,
    decimal Amount,
    string TransactionType,
    string? Description,
    string? ReferenceId,
    DateTimeOffset CreatedAt);

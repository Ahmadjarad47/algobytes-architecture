namespace algo.Application.Features.Orders.Dtos;

public sealed record PaymentDto(
    long Id,
    long OrderId,
    string CurrencyCode,
    string GatewayName,
    string GatewayTransactionId,
    decimal Amount,
    string PaymentStatus);

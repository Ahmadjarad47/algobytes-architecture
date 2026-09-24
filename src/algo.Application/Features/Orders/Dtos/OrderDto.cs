namespace algo.Application.Features.Orders.Dtos;

public sealed record OrderDto(
    long Id,
    string UserId,
    string OrderNumber,
    string CurrencyCode,
    decimal TotalAmount,
    decimal? ExchangeRateUsedToBase,
    string? PaymentMethod,
    string OrderStatus,
    DateTimeOffset CreatedAt,
    IReadOnlyList<OrderItemDto> Items,
    IReadOnlyList<PaymentDto> Payments);

using System.Text.Json;

namespace algo.Application.Features.Shop.Orders.Dtos;

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
    JsonElement? CustomFields,
    IReadOnlyList<OrderItemDto> Items,
    IReadOnlyList<PaymentDto> Payments);

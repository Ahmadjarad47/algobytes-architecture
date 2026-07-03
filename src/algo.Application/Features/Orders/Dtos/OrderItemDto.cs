namespace algo.Application.Features.Orders.Dtos;

public sealed record OrderItemDto(
    long Id,
    long OrderId,
    int ProductId,
    int Quantity,
    decimal UnitPrice);

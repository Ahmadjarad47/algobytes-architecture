using System.Text.Json;
using algo.Application.Features.Shop.Orders.Dtos;
using MediatR;

namespace algo.Application.Features.Shop.Orders.Commands.CreateOrder;

public sealed record CreateOrderCommand(
    string OrderNumber,
    string? PaymentMethod,
    decimal? ExchangeRateUsedToBase,
    IReadOnlyList<CreateOrderItemInput> Items,
    JsonDocument? CustomFields) : IRequest<OrderDto>;

public sealed record CreateOrderItemInput(
    int ProductId,
    int Quantity);

using algo.Application.Features.Orders.Dtos;
using MediatR;

namespace algo.Application.Features.Orders.Commands.CreateOrder;

public sealed record CreateOrderCommand(
    string? UserId,
    string OrderNumber,
    string CurrencyCode,
    decimal TotalAmount,
    decimal? ExchangeRateUsedToBase,
    string? PaymentMethod,
    string? OrderStatus,
    IReadOnlyList<CreateOrderItemModel> Items,
    IReadOnlyList<CreatePaymentModel>? Payments) : IRequest<OrderDto>;

public sealed record CreateOrderItemModel(
    int ProductId,
    int Quantity,
    decimal UnitPrice);

public sealed record CreatePaymentModel(
    string CurrencyCode,
    string GatewayName,
    string GatewayTransactionId,
    decimal Amount,
    string PaymentStatus);

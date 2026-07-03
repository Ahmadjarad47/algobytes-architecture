using algo.Application.Abstractions.Identity;
using algo.Application.Abstractions.Persistence;
using algo.Application.Features.Orders.Dtos;
using algo.Domain.Sales.Entities;
using MediatR;

namespace algo.Application.Features.Orders.Commands.CreateOrder;

public sealed class CreateOrderCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<CreateOrderCommand, OrderDto>
{
    public async Task<OrderDto> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var userId = string.IsNullOrWhiteSpace(request.UserId)
            ? currentUser.UserId
            : request.UserId.Trim();

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new InvalidOperationException("User id is required to create an order.");
        }

        var order = new Order
        {
            UserId = userId,
            OrderNumber = request.OrderNumber.Trim(),
            CurrencyCode = request.CurrencyCode.Trim(),
            TotalAmount = request.TotalAmount,
            ExchangeRateUsedToBase = request.ExchangeRateUsedToBase,
            PaymentMethod = string.IsNullOrWhiteSpace(request.PaymentMethod)
                ? null
                : request.PaymentMethod.Trim(),
            OrderStatus = string.IsNullOrWhiteSpace(request.OrderStatus)
                ? "Pending"
                : request.OrderStatus.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
            OrderItems = request.Items
                .Select(item => new OrderItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                })
                .ToList(),
            Payments = (request.Payments ?? Array.Empty<CreatePaymentModel>())
                .Select(payment => new Payment
                {
                    CurrencyCode = payment.CurrencyCode.Trim(),
                    GatewayName = payment.GatewayName.Trim(),
                    GatewayTransactionId = payment.GatewayTransactionId.Trim(),
                    Amount = payment.Amount,
                    PaymentStatus = payment.PaymentStatus.Trim(),
                })
                .ToList(),
        };

        db.Orders.Add(order);
        await db.SaveChangesAsync(cancellationToken);

        return MapOrder(order);
    }

    private static OrderDto MapOrder(Order order) =>
        new(
            order.Id,
            order.UserId,
            order.OrderNumber,
            order.CurrencyCode,
            order.TotalAmount,
            order.ExchangeRateUsedToBase,
            order.PaymentMethod,
            order.OrderStatus,
            order.CreatedAt,
            order.OrderItems
                .Select(item => new OrderItemDto(
                    item.Id,
                    item.OrderId,
                    item.ProductId,
                    item.Quantity,
                    item.UnitPrice))
                .ToList(),
            order.Payments
                .Select(payment => new PaymentDto(
                    payment.Id,
                    payment.OrderId,
                    payment.CurrencyCode,
                    payment.GatewayName,
                    payment.GatewayTransactionId,
                    payment.Amount,
                    payment.PaymentStatus))
                .ToList());
}

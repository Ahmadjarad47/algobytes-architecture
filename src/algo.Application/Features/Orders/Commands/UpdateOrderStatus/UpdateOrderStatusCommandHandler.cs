using algo.Application.Abstractions.Persistence;
using algo.Application.Features.Orders.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace algo.Application.Features.Orders.Commands.UpdateOrderStatus;

public sealed class UpdateOrderStatusCommandHandler(IApplicationDbContext db)
    : IRequestHandler<UpdateOrderStatusCommand, OrderDto?>
{
    private static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Pending",
        "Paid",
        "Failed",
        "Processing",
    };

    public async Task<OrderDto?> Handle(UpdateOrderStatusCommand request, CancellationToken cancellationToken)
    {
        if (!AllowedStatuses.Contains(request.OrderStatus))
        {
            throw new InvalidOperationException("Invalid order status.");
        }

        var order = await db.Orders
            .Include(o => o.OrderItems)
            .Include(o => o.Payments)
            .FirstOrDefaultAsync(o => o.Id == request.Id, cancellationToken);

        if (order is null)
        {
            return null;
        }

        order.OrderStatus = request.OrderStatus.Trim();
        await db.SaveChangesAsync(cancellationToken);

        return new OrderDto(
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
}

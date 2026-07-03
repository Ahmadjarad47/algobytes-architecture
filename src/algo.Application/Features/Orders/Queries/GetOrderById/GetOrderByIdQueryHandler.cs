using algo.Application.Abstractions.Persistence;
using algo.Application.Features.Orders.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace algo.Application.Features.Orders.Queries.GetOrderById;

public sealed class GetOrderByIdQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetOrderByIdQuery, OrderDto?>
{
    public async Task<OrderDto?> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        return await db.Orders
            .AsNoTracking()
            .Include(order => order.OrderItems)
            .Include(order => order.Payments)
            .Where(order => order.Id == request.Id)
            .Select(order => new OrderDto(
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
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);
    }
}

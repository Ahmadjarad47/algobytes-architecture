using algo.Application.Abstractions;
using algo.Application.Common.CustomFields;
using algo.Application.Features.Shop.Orders.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace algo.Application.Features.Shop.Orders.Queries.GetAllOrders;

public sealed class GetAllOrdersQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetAllOrdersQuery, IReadOnlyList<OrderDto>>
{
    public async Task<IReadOnlyList<OrderDto>> Handle(GetAllOrdersQuery request, CancellationToken cancellationToken)
    {
        return await db.Orders
            .AsNoTracking()
            .Include(order => order.OrderItems)
            .Include(order => order.Payments)
            .OrderByDescending(order => order.CreatedAt)
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
                JsonDocumentHelpers.CloneToElement(order.CustomFields),
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
            .ToListAsync(cancellationToken);
    }
}

using algo.Application.Abstractions;
using algo.Application.Common.CustomFields;
using algo.Application.Features.Shop.Orders.Dtos;
using algo.Domain.Catalog.Entities;
using algo.Domain.Sales.Entities;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace algo.Application.Features.Shop.Orders.Commands.CreateOrder;

public sealed class CreateOrderCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    CustomFieldValueValidator customFieldValueValidator)
    : IRequestHandler<CreateOrderCommand, OrderDto>
{
    public async Task<OrderDto> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(currentUser.UserId))
        {
            throw new UnauthorizedAccessException("Current user is not available.");
        }

        if (request.Items.Count == 0)
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(request.Items), "At least one order item is required."),
            });
        }

        var productIds = request.Items.Select(item => item.ProductId).Distinct().ToArray();
        var productsById = await db.Products
            .AsNoTracking()
            .Where(product => productIds.Contains(product.Id))
            .ToDictionaryAsync(product => product.Id, cancellationToken);

        var missingProductIds = productIds.Where(id => !productsById.ContainsKey(id)).ToArray();
        if (missingProductIds.Length > 0)
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(request.Items), $"Products not found: {string.Join(", ", missingProductIds)}."),
            });
        }

        var currencies = request.Items
            .Select(item => productsById[item.ProductId].CurrencyCode)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (currencies.Length != 1)
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(request.Items), "All products in an order must use the same currency."),
            });
        }

        var orderItems = request.Items.Select(item =>
        {
            var product = productsById[item.ProductId];
            var unitPrice = ResolveUnitPrice(product);
            return new OrderItem
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = unitPrice,
            };
        }).ToList();

        var order = new Order
        {
            UserId = currentUser.UserId!,
            OrderNumber = request.OrderNumber.Trim(),
            CurrencyCode = currencies[0].Trim().ToUpperInvariant(),
            TotalAmount = orderItems.Sum(item => item.UnitPrice * item.Quantity),
            ExchangeRateUsedToBase = request.ExchangeRateUsedToBase,
            PaymentMethod = string.IsNullOrWhiteSpace(request.PaymentMethod)
                ? null
                : request.PaymentMethod.Trim(),
            OrderStatus = "Pending",
            CreatedAt = DateTimeOffset.UtcNow,
            CustomFields = await customFieldValueValidator.ValidateAndNormalizeAsync(
                CustomFieldEntities.Orders,
                JsonDocumentHelpers.CloneToElement(request.CustomFields),
                cancellationToken),
            OrderItems = orderItems,
        };

        db.Orders.Add(order);
        await db.SaveChangesAsync(cancellationToken);

        return Map(order);
    }

    private static decimal ResolveUnitPrice(Product product) => product.DiscountedPrice ?? product.Price;

    private static OrderDto Map(Order order) =>
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
                .ToList());
}

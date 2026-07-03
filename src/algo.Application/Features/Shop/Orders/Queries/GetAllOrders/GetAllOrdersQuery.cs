using algo.Application.Features.Shop.Orders.Dtos;
using MediatR;

namespace algo.Application.Features.Shop.Orders.Queries.GetAllOrders;

public sealed record GetAllOrdersQuery : IRequest<IReadOnlyList<OrderDto>>;

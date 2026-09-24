using algo.Application.Features.Orders.Dtos;
using MediatR;

namespace algo.Application.Features.Orders.Queries.GetAllOrders;

public sealed record GetAllOrdersQuery() : IRequest<IReadOnlyList<OrderDto>>;

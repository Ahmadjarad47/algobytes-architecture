using algo.Application.Features.Shop.Orders.Dtos;
using MediatR;

namespace algo.Application.Features.Shop.Orders.Queries.GetOrderById;

public sealed record GetOrderByIdQuery(long Id) : IRequest<OrderDto?>;

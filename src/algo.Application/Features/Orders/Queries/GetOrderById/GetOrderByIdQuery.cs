using algo.Application.Features.Orders.Dtos;
using MediatR;

namespace algo.Application.Features.Orders.Queries.GetOrderById;

public sealed record GetOrderByIdQuery(long Id) : IRequest<OrderDto?>;

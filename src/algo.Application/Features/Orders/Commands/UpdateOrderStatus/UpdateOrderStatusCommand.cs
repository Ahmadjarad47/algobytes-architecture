using algo.Application.Features.Orders.Dtos;
using MediatR;

namespace algo.Application.Features.Orders.Commands.UpdateOrderStatus;

public sealed record UpdateOrderStatusCommand(
    long Id,
    string OrderStatus) : IRequest<OrderDto?>;

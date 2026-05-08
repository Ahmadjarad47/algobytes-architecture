using algo.Application.Features.Roles.Dtos;
using MediatR;

namespace algo.Application.Features.Roles.Queries.GetRoleById;

public sealed record GetRoleByIdQuery(string Id) : IRequest<RoleDetailsDto?>;

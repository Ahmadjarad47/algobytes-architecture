using algo.Application.Features.Roles.Dtos;
using MediatR;

namespace algo.Application.Features.Roles.Commands.UpdateRole;

public sealed record UpdateRoleCommand(string Id, string Name) : IRequest<RoleDetailsDto?>;

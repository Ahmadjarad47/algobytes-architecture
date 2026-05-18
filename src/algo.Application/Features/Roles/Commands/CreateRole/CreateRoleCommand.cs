using System.Text.Json;
using algo.Application.Features.Roles.Dtos;
using MediatR;

namespace algo.Application.Features.Roles.Commands.CreateRole;

public sealed record CreateRoleCommand(string Name, JsonElement? CustomFields) : IRequest<RoleDetailsDto>;

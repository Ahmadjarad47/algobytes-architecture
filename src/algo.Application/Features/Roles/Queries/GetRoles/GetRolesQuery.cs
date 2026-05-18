using algo.Application.Features.Roles.Dtos;
using MediatR;

namespace algo.Application.Features.Roles.Queries.GetRoles;

public sealed record GetRolesQuery(
    bool IncludeTrashed = false,
    bool OnlyTrashed = false) : IRequest<IReadOnlyList<RoleDto>>;

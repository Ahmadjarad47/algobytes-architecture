using algo.Application.Features.Users.Dtos;
using MediatR;

namespace algo.Application.Features.Users.Queries.GetUserRoles;

public sealed record GetUserRolesQuery(string UserId) : IRequest<IReadOnlyList<UserRoleDto>>;

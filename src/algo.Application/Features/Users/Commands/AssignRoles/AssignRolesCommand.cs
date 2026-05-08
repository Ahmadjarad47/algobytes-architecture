using MediatR;

namespace algo.Application.Features.Users.Commands.AssignRoles;

public sealed record AssignRolesCommand(string UserId, IReadOnlyList<string> RoleNames) : IRequest<Unit>;

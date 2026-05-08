using MediatR;

namespace algo.Application.Features.Users.Commands.RemoveRoles;

public sealed record RemoveRolesCommand(string UserId, IReadOnlyList<string> RoleNames) : IRequest<Unit>;

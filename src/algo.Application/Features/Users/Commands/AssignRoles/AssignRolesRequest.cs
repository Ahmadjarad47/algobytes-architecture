namespace algo.Application.Features.Users.Commands.AssignRoles;

public sealed record AssignRolesRequest(IReadOnlyList<string> Roles);

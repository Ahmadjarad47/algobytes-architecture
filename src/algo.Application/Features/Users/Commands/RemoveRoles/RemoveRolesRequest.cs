namespace algo.Application.Features.Users.Commands.RemoveRoles;

public sealed record RemoveRolesRequest(IReadOnlyList<string> Roles);

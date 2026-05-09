namespace algo.Application.Features.Sessions.Commands.RevokeSelectedSessions;

public sealed record RevokeSelectedSessionsRequest(IReadOnlyList<Guid> Ids);

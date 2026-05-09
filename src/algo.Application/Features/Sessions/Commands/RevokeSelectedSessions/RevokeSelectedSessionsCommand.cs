using MediatR;

namespace algo.Application.Features.Sessions.Commands.RevokeSelectedSessions;

public sealed record RevokeSelectedSessionsCommand(IReadOnlyList<Guid> Ids) : IRequest<int>;

using MediatR;

namespace algo.Application.Features.Sessions.Commands.RevokeUserSessions;

public sealed record RevokeUserSessionsCommand(string UserId, bool ConfirmCurrentUser = false) : IRequest<int>;

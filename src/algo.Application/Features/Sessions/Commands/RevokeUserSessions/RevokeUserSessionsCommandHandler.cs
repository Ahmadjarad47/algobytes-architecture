using algo.Application.Abstractions;
using algo.Application.Common.AccessPolicy;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace algo.Application.Features.Sessions.Commands.RevokeUserSessions;

public sealed class RevokeUserSessionsCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IAccessPolicyEvaluator accessPolicyEvaluator,
    ISessionRealtimeNotifier sessionRealtimeNotifier) : IRequestHandler<RevokeUserSessionsCommand, int>
{
    public async Task<int> Handle(RevokeUserSessionsCommand request, CancellationToken cancellationToken)
    {
        await accessPolicyEvaluator.EnsureResourceActionAllowedAsync(
            db,
            AccessPolicyResources.Sessions,
            AccessPolicyActions.RevokeAll,
            cancellationToken);

        if (request.UserId == currentUser.UserId && !request.ConfirmCurrentUser)
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(
                    nameof(RevokeUserSessionsCommand.ConfirmCurrentUser),
                    "Revoking all sessions for the current admin requires confirmation."),
            });
        }

        var sessions = await db.RefreshTokens
            .Where(t => t.UserId == request.UserId && t.RevokedAt == null && t.ExpiresAt > DateTimeOffset.UtcNow)
            .ToListAsync(cancellationToken);

        foreach (var session in sessions)
        {
            session.RevokedAt = DateTimeOffset.UtcNow;
            session.RevokedByUserId = currentUser.UserId;
        }

        await db.SaveChangesAsync(cancellationToken);
        await sessionRealtimeNotifier.NotifySessionsRevokedAsync(
            sessions.Select(s => s.Id).ToArray(),
            new[] { request.UserId },
            cancellationToken);

        return sessions.Count;
    }
}

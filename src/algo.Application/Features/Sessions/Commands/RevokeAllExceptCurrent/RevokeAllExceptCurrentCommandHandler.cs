using algo.Application.Abstractions;
using algo.Application.Common.AccessPolicy;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace algo.Application.Features.Sessions.Commands.RevokeAllExceptCurrent;

public sealed class RevokeAllExceptCurrentCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IAccessPolicyEvaluator accessPolicyEvaluator,
    ISessionRealtimeNotifier sessionRealtimeNotifier) : IRequestHandler<RevokeAllExceptCurrentCommand, int>
{
    public async Task<int> Handle(RevokeAllExceptCurrentCommand request, CancellationToken cancellationToken)
    {
        await accessPolicyEvaluator.EnsureResourceActionAllowedAsync(
            db,
            AccessPolicyResources.Sessions,
            AccessPolicyActions.RevokeAll,
            cancellationToken);

        if (!string.Equals(request.Confirmation, "LOGOUT", StringComparison.Ordinal))
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(RevokeAllExceptCurrentCommand.Confirmation), "Type LOGOUT to confirm."),
            });
        }

        var sessions = await db.RefreshTokens
            .Where(t =>
                t.UserId != currentUser.UserId &&
                t.RevokedAt == null &&
                t.ExpiresAt > DateTimeOffset.UtcNow)
            .ToListAsync(cancellationToken);

        foreach (var session in sessions)
        {
            session.RevokedAt = DateTimeOffset.UtcNow;
            session.RevokedByUserId = currentUser.UserId;
        }

        await db.SaveChangesAsync(cancellationToken);
        await sessionRealtimeNotifier.NotifySessionsRevokedAsync(
            sessions.Select(s => s.Id).ToArray(),
            sessions.Select(s => s.UserId).Distinct(StringComparer.Ordinal).ToArray(),
            cancellationToken);

        return sessions.Count;
    }
}

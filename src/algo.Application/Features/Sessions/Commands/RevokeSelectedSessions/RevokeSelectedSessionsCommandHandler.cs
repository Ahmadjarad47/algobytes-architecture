using algo.Application.Abstractions;
using algo.Application.Common.AccessPolicy;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace algo.Application.Features.Sessions.Commands.RevokeSelectedSessions;

public sealed class RevokeSelectedSessionsCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IAccessPolicyEvaluator accessPolicyEvaluator,
    ISessionRealtimeNotifier sessionRealtimeNotifier) : IRequestHandler<RevokeSelectedSessionsCommand, int>
{
    public async Task<int> Handle(RevokeSelectedSessionsCommand request, CancellationToken cancellationToken)
    {
        await accessPolicyEvaluator.EnsureResourceActionAllowedAsync(
            db,
            AccessPolicyResources.Sessions,
            AccessPolicyActions.Revoke,
            cancellationToken);

        var idSet = request.Ids.ToHashSet();
        var sessions = await db.RefreshTokens
            .Where(t => idSet.Contains(t.Id) && t.RevokedAt == null && t.ExpiresAt > DateTimeOffset.UtcNow)
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

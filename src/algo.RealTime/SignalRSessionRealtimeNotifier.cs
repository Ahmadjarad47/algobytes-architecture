using algo.Application.Abstractions;
using Microsoft.AspNetCore.SignalR;

namespace algo.RealTime;

public sealed class SignalRSessionRealtimeNotifier(IHubContext<SessionHub> hubContext) : ISessionRealtimeNotifier
{
    public async Task NotifySessionsRevokedAsync(
        IReadOnlyCollection<Guid> sessionIds,
        IReadOnlyCollection<string> userIds,
        CancellationToken cancellationToken)
    {
        if (sessionIds.Count == 0 && userIds.Count == 0)
        {
            return;
        }

        foreach (var sessionId in sessionIds)
        {
            await hubContext.Clients.Group(PresenceGroupNames.Session(sessionId)).SendAsync(
                "forceLogout",
                new { reason = "Session revoked by administrator.", revokedSessionId = sessionId.ToString("D") },
                cancellationToken);
        }

        foreach (var userId in userIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.Ordinal))
        {
            await hubContext.Clients.Group(PresenceGroupNames.User(userId)).SendAsync(
                "forceLogout",
                new { reason = "All your sessions were revoked by administrator." },
                cancellationToken);
        }
    }
}

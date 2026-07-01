namespace algo.Application.Abstractions.Identity;

public interface ISessionRealtimeNotifier
{
    Task NotifySessionsRevokedAsync(
        IReadOnlyCollection<Guid> sessionIds,
        IReadOnlyCollection<string> userIds,
        CancellationToken cancellationToken);
}


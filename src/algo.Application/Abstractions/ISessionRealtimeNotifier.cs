namespace algo.Application.Abstractions;

public interface ISessionRealtimeNotifier
{
    Task NotifySessionsRevokedAsync(
        IReadOnlyCollection<Guid> sessionIds,
        IReadOnlyCollection<string> userIds,
        CancellationToken cancellationToken);
}

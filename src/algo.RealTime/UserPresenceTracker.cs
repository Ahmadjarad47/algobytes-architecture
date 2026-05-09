using System.Collections.Concurrent;

namespace algo.RealTime;

public sealed class UserPresenceTracker
{
    private readonly ConcurrentDictionary<string, int> _connectionsByUser = new(StringComparer.Ordinal);

    public bool SetConnected(string userId)
    {
        var count = _connectionsByUser.AddOrUpdate(userId, 1, (_, current) => current + 1);
        return count == 1;
    }

    public bool SetDisconnected(string userId)
    {
        while (true)
        {
            if (!_connectionsByUser.TryGetValue(userId, out var current))
            {
                return false;
            }

            if (current <= 1)
            {
                return _connectionsByUser.TryRemove(userId, out _);
            }

            if (_connectionsByUser.TryUpdate(userId, current - 1, current))
            {
                return false;
            }
        }
    }

    public IReadOnlyCollection<string> OnlineUserIds() => _connectionsByUser.Keys.ToArray();
}

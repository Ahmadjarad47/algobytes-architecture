using StackExchange.Redis;

namespace algo.RealTime;

public sealed class UserPresenceTracker(IConnectionMultiplexer redis)
{
    private const string OnlineUsersSetKey = "presence:online-users";
    private const string UserConnectionsCounterPrefix = "presence:user:";
    private readonly IDatabase _db = redis.GetDatabase();

    public async Task<bool> SetConnectedAsync(string userId)
    {
        var counterKey = BuildCounterKey(userId);
        var count = await _db.StringIncrementAsync(counterKey).ConfigureAwait(false);
        await _db.SetAddAsync(OnlineUsersSetKey, userId).ConfigureAwait(false);
        return count == 1;
    }

    public async Task<bool> SetDisconnectedAsync(string userId)
    {
        var counterKey = BuildCounterKey(userId);
        var count = await _db.StringDecrementAsync(counterKey).ConfigureAwait(false);

        if (count <= 0)
        {
            await _db.KeyDeleteAsync(counterKey).ConfigureAwait(false);
            return await _db.SetRemoveAsync(OnlineUsersSetKey, userId).ConfigureAwait(false);
        }

        return false;
    }

    public async Task<IReadOnlyCollection<string>> OnlineUserIdsAsync()
    {
        var members = await _db.SetMembersAsync(OnlineUsersSetKey).ConfigureAwait(false);
        return members.Select(x => (string)x!).ToArray();
    }

    private static string BuildCounterKey(string userId) => $"{UserConnectionsCounterPrefix}{userId}:connections";
}

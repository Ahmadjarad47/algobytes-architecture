using System.Text.Json;
using StackExchange.Redis;

namespace algo.RealTime;

public sealed class RedisRealtimeChatStore(IConnectionMultiplexer multiplexer) : IRealtimeChatStore
{
    private const string ChatTimelineKeyPrefix = "realtime:dm:timeline:";
    private const string ChatMessageKeyPrefix = "realtime:dm:message:";
    private static readonly TimeSpan RetentionPeriod = TimeSpan.FromDays(3);
    private const int MaxMessageCount = 250;
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly IDatabase _database = multiplexer.GetDatabase();

    public async Task<IReadOnlyList<ChatMessage>> GetConversationAsync(string userAId, string userBId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var timelineKey = BuildTimelineKey(userAId, userBId);
        await PruneExpiredMessagesAsync(timelineKey, cancellationToken);

        var messageIds = await _database.SortedSetRangeByScoreAsync(
            timelineKey,
            order: Order.Descending,
            take: MaxMessageCount);
        var messages = new List<ChatMessage>(messageIds.Length);

        foreach (var idEntry in messageIds)
        {
            if (!idEntry.HasValue)
            {
                continue;
            }

            var payload = await _database.StringGetAsync(BuildMessageKey(idEntry.ToString()));
            if (!payload.HasValue)
            {
                continue;
            }

            var parsed = JsonSerializer.Deserialize<ChatMessage>(payload.ToString(), SerializerOptions);
            if (parsed is not null)
            {
                messages.Add(parsed);
            }
        }

        messages.Sort((a, b) => a.SentAtUtc.CompareTo(b.SentAtUtc));
        return messages;
    }

    public async Task<ChatMessage> AppendDirectMessageAsync(ChatMessage message, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var timelineKey = BuildTimelineKey(message.SenderUserId, message.RecipientUserId);
        await PruneExpiredMessagesAsync(timelineKey, cancellationToken);

        var payload = JsonSerializer.Serialize(message, SerializerOptions);
        var messageKey = BuildMessageKey(message.Id);
        var score = message.SentAtUtc.ToUnixTimeSeconds();
        var ttl = ResolveRemainingRetention(message.SentAtUtc);

        var batch = _database.CreateBatch();
        var setMessageTask = batch.StringSetAsync(messageKey, payload, ttl);
        var addTimelineTask = batch.SortedSetAddAsync(timelineKey, message.Id, score);
        batch.Execute();

        await Task.WhenAll(setMessageTask, addTimelineTask);
        return message;
    }

    public async Task<ChatReadReceipt> MarkConversationAsReadAsync(string readerUserId, string counterpartUserId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var timelineKey = BuildTimelineKey(readerUserId, counterpartUserId);
        await PruneExpiredMessagesAsync(timelineKey, cancellationToken);

        var messageIds = await _database.SortedSetRangeByScoreAsync(timelineKey, order: Order.Ascending, take: MaxMessageCount);
        if (messageIds.Length == 0)
        {
            return new ChatReadReceipt(readerUserId, counterpartUserId, [], DateTimeOffset.UtcNow);
        }

        var readAt = DateTimeOffset.UtcNow;
        var updatedMessageIds = new List<string>();
        var batch = _database.CreateBatch();
        var updateTasks = new List<Task<bool>>();

        foreach (var idEntry in messageIds)
        {
            if (!idEntry.HasValue)
            {
                continue;
            }

            var messageId = idEntry.ToString();
            var messageKey = BuildMessageKey(messageId);
            var payload = await _database.StringGetAsync(messageKey);
            if (!payload.HasValue)
            {
                continue;
            }

            var parsed = JsonSerializer.Deserialize<ChatMessage>(payload.ToString(), SerializerOptions);
            if (parsed is null ||
                parsed.RecipientUserId != readerUserId ||
                parsed.IsRead)
            {
                continue;
            }

            var updated = parsed with
            {
                IsRead = true,
                ReadAtUtc = readAt
            };
            var updatedPayload = JsonSerializer.Serialize(updated, SerializerOptions);
            updateTasks.Add(batch.StringSetAsync(messageKey, updatedPayload, ResolveRemainingRetention(parsed.SentAtUtc)));
            updatedMessageIds.Add(messageId);
        }

        batch.Execute();
        if (updateTasks.Count > 0)
        {
            await Task.WhenAll(updateTasks);
        }

        return new ChatReadReceipt(readerUserId, counterpartUserId, updatedMessageIds, readAt);
    }

    private async Task PruneExpiredMessagesAsync(string timelineKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var cutoff = DateTimeOffset.UtcNow.Subtract(RetentionPeriod).ToUnixTimeSeconds();
        var expiredMessageIds = await _database.SortedSetRangeByScoreAsync(timelineKey, stop: cutoff);
        if (expiredMessageIds.Length == 0)
        {
            return;
        }

        var batch = _database.CreateBatch();
        var timelineCleanupTask = batch.SortedSetRemoveRangeByScoreAsync(timelineKey, double.NegativeInfinity, cutoff);
        var deleteTasks = new List<Task<bool>>(expiredMessageIds.Length);

        foreach (var idEntry in expiredMessageIds)
        {
            if (idEntry.HasValue)
            {
                deleteTasks.Add(batch.KeyDeleteAsync(BuildMessageKey(idEntry.ToString())));
            }
        }

        batch.Execute();
        await timelineCleanupTask;
        await Task.WhenAll(deleteTasks);
    }

    private static string BuildMessageKey(string messageId) => $"{ChatMessageKeyPrefix}{messageId}";
    private static TimeSpan ResolveRemainingRetention(DateTimeOffset sentAtUtc)
    {
        var remaining = sentAtUtc.Add(RetentionPeriod) - DateTimeOffset.UtcNow;
        return remaining > TimeSpan.FromMinutes(1) ? remaining : TimeSpan.FromMinutes(1);
    }

    private static string BuildTimelineKey(string userAId, string userBId)
    {
        var ordered = string.CompareOrdinal(userAId, userBId) <= 0
            ? $"{userAId}:{userBId}"
            : $"{userBId}:{userAId}";
        return $"{ChatTimelineKeyPrefix}{ordered}";
    }
}

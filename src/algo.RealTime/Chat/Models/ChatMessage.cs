namespace algo.RealTime;

public sealed record ChatMessage(
    string Id,
    string SenderUserId,
    string RecipientUserId,
    string SenderDisplayName,
    bool SenderIsAdmin,
    string Content,
    DateTimeOffset SentAtUtc,
    string? ReplyToMessageId,
    bool IsRead,
    DateTimeOffset? ReadAtUtc);

namespace algo.RealTime;

public sealed record ChatTypingEvent(
    string UserId,
    string DisplayName,
    bool IsAdmin,
    bool IsTyping,
    DateTimeOffset TimestampUtc);

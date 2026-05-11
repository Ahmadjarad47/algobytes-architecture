namespace algo.RealTime;

public sealed record ChatReadReceipt(
    string ReaderUserId,
    string CounterpartUserId,
    IReadOnlyList<string> MessageIds,
    DateTimeOffset ReadAtUtc);

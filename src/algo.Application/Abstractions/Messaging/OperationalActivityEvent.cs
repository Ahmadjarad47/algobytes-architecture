namespace algo.Application.Abstractions.Messaging;

public sealed record OperationalActivityEvent(
    DateTimeOffset Timestamp,
    string Level,
    string Source,
    string Message,
    string? TraceId = null,
    string? UserId = null,
    int? StatusCode = null,
    long? DurationMs = null);


namespace algo.Application.Features.Logs.Dtos;

public sealed record ApplicationLogDto(
    long Id,
    DateTimeOffset Timestamp,
    string Level,
    string Message,
    string? MessageTemplate,
    string? Exception,
    string? Properties,
    string? TraceId,
    string? UserId,
    string? UserName,
    string? RequestPath,
    string? RequestMethod,
    int? StatusCode,
    long? ElapsedMilliseconds);

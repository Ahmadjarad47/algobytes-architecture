namespace algo.Application.Features.Logs.Dtos;

public sealed record LogFilterDto(
    string? Level = null,
    DateTimeOffset? FromTimestamp = null,
    DateTimeOffset? ToTimestamp = null,
    string? UserId = null,
    string? UserName = null,
    string? TraceId = null,
    string? RequestPath = null,
    string? RequestMethod = null,
    string? MessageContains = null);

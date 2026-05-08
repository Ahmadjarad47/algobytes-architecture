namespace algo.Application.Features.ErrorLogs.Dtos;

public sealed record ErrorLogFilterDto(
    string? ExceptionType = null,
    int? StatusCode = null,
    DateTimeOffset? FromTimestamp = null,
    DateTimeOffset? ToTimestamp = null,
    string? UserId = null,
    string? UserName = null,
    string? TraceId = null,
    string? Path = null,
    string? Method = null,
    string? MessageContains = null);

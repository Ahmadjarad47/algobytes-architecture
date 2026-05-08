namespace algo.Application.Features.ErrorLogs.Dtos;

public sealed record ErrorLogDto(
    long Id,
    DateTimeOffset Timestamp,
    string Level,
    string ExceptionType,
    string Message,
    string? StackTrace,
    string? Source,
    string? Path,
    string? Method,
    int StatusCode,
    string? TraceId,
    string? UserId,
    string? UserName,
    string? RequestBody,
    string? QueryString,
    string? Headers,
    string Environment,
    string MachineName,
    DateTimeOffset CreatedAt);

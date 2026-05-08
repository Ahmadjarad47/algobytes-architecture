namespace algo.Domain.Logging.Entities;

public sealed class ErrorLog
{
    public long Id { get; set; }

    public DateTimeOffset Timestamp { get; set; }

    public string Level { get; set; } = "Error";

    public string ExceptionType { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string? StackTrace { get; set; }

    public string? Source { get; set; }

    public string? Path { get; set; }

    public string? Method { get; set; }

    public int StatusCode { get; set; }

    public string? TraceId { get; set; }

    public string? UserId { get; set; }

    public string? UserName { get; set; }

    public string? RequestBody { get; set; }

    public string? QueryString { get; set; }

    public string? Headers { get; set; }

    public string Environment { get; set; } = string.Empty;

    public string MachineName { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }
}

namespace algo.SharedKernel.Constants;

/// <summary>Structured logging property names for request context and the PostgreSQL sink.</summary>
public static class LoggingConstants
{
    public const string TraceId = "TraceId";

    public const string UserId = "UserId";

    public const string UserName = "UserName";

    public const string RequestPath = "RequestPath";

    public const string RequestMethod = "RequestMethod";

    public const string StatusCode = "StatusCode";

    public const string ElapsedMilliseconds = "ElapsedMilliseconds";
}

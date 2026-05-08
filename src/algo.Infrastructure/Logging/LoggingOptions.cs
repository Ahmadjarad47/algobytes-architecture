namespace algo.Infrastructure.Logging;

public sealed class LoggingOptions
{
    public const string SectionName = "Logging";

    public string ApplicationLogsTableName { get; set; } = "ApplicationLogs";
}

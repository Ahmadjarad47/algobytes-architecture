using algo.Infrastructure.Logging;
using Microsoft.Extensions.Options;
using NpgsqlTypes;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.PostgreSQL;

namespace algo.API.Configuration;

public static class SerilogConfiguration
{
    private static readonly Dictionary<string, ColumnWriterBase> ApplicationLogColumnWriters = new()
    {
        ["Timestamp"] = new TimestampColumnWriter(),
        ["Level"] = new LevelColumnWriter(true, NpgsqlDbType.Text),
        ["Message"] = new RenderedMessageColumnWriter(),
        ["MessageTemplate"] = new MessageTemplateColumnWriter(),
        ["Exception"] = new ExceptionColumnWriter(),
        ["Properties"] = new PropertiesColumnWriter(),
        ["TraceId"] = new SinglePropertyColumnWriter("TraceId", PropertyWriteMethod.ToString, NpgsqlDbType.Text),
        ["UserId"] = new SinglePropertyColumnWriter("UserId", PropertyWriteMethod.ToString, NpgsqlDbType.Text),
        ["UserName"] = new SinglePropertyColumnWriter("UserName", PropertyWriteMethod.ToString, NpgsqlDbType.Text),
        ["RequestPath"] = new SinglePropertyColumnWriter("RequestPath", PropertyWriteMethod.ToString, NpgsqlDbType.Text),
        ["RequestMethod"] = new SinglePropertyColumnWriter("RequestMethod", PropertyWriteMethod.ToString, NpgsqlDbType.Text),
        ["StatusCode"] = new SinglePropertyColumnWriter("StatusCode", PropertyWriteMethod.Raw, NpgsqlDbType.Integer),
        ["ElapsedMilliseconds"] = new SinglePropertyColumnWriter(
            "ElapsedMilliseconds",
            PropertyWriteMethod.Raw,
            NpgsqlDbType.Bigint),
    };

    public static void Configure(HostBuilderContext host, IServiceProvider services, LoggerConfiguration loggerConfiguration)
    {
        var configuration = host.Configuration;
        var connectionString = configuration.GetConnectionString("Database")
            ?? throw new InvalidOperationException("Connection string 'Database' is required for Serilog PostgreSQL sink.");

        var loggingOptions = services.GetService<IOptions<LoggingOptions>>()?.Value ?? new LoggingOptions();
        var tableName = string.IsNullOrWhiteSpace(loggingOptions.ApplicationLogsTableName)
            ? "ApplicationLogs"
            : loggingOptions.ApplicationLogsTableName;

        loggerConfiguration
            .ReadFrom.Configuration(configuration)
            .Enrich.With(services.GetRequiredService<LoggingEnricher>())
            .WriteTo.PostgreSQL(
                connectionString,
                tableName,
                ApplicationLogColumnWriters,
                LogEventLevel.Information,
                period: TimeSpan.FromSeconds(5),
                formatProvider: null,
                batchSizeLimit: 50,
                levelSwitch: null,
                useCopy: false,
                schemaName: "public",
                needAutoCreateTable: false,
                respectCase: true);
    }
}

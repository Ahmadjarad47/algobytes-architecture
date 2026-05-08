using algo.API.Middleware;

namespace algo.API.Extensions;

public static class LoggingApplicationBuilderExtensions
{
    public static IApplicationBuilder UseAlgoStructuredLogging(this IApplicationBuilder app) =>
        app.UseMiddleware<ExceptionHandlingMiddleware>()
            .UseMiddleware<RequestLoggingMiddleware>();
}

using System.Diagnostics;

namespace algo.API.Middleware;

public sealed class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();
        var method = context.Request.Method;
        var path = context.Request.Path.Value;
        var traceId = context.TraceIdentifier;

        try
        {
            await next(context);
        }
        finally
        {
            sw.Stop();
            logger.LogInformation(
                "HTTP {RequestMethod} {RequestPath} completed with {StatusCode} in {ElapsedMilliseconds} ms ({TraceId})",
                method,
                path,
                context.Response.StatusCode,
                sw.ElapsedMilliseconds,
                traceId);
        }
    }
}

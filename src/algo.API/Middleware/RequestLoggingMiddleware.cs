using System.Diagnostics;
using System.Security.Claims;
using algo.RealTime;

namespace algo.API.Middleware;

public sealed class RequestLoggingMiddleware(
    RequestDelegate next,
    ILogger<RequestLoggingMiddleware> logger,
    IOperationalActivityNotifier operationalActivityNotifier)
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

            if (!context.Request.Path.StartsWithSegments("/hubs", StringComparison.OrdinalIgnoreCase))
            {
                await operationalActivityNotifier.NotifyAsync(new OperationalActivityEvent(
                    DateTimeOffset.UtcNow,
                    context.Response.StatusCode >= 500 ? "error" : context.Response.StatusCode >= 400 ? "warn" : "info",
                    "http",
                    $"{method} {path} -> {context.Response.StatusCode} in {sw.ElapsedMilliseconds} ms",
                    traceId,
                    context.User.FindFirstValue("sub") ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier),
                    context.Response.StatusCode,
                    sw.ElapsedMilliseconds),
                    context.RequestAborted);
            }
        }
    }
}

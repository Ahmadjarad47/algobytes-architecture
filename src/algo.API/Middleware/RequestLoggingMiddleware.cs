using System.Diagnostics;
using System.Security.Claims;
using algo.Application.Abstractions;
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
        var failed = false;

        try
        {
            await next(context);
        }
        catch
        {
            failed = true;
            throw;
        }
        finally
        {
            sw.Stop();
            var statusCode = failed && context.Response.StatusCode < 500
                ? StatusCodes.Status500InternalServerError
                : context.Response.StatusCode;
            logger.LogInformation(
                "HTTP {RequestMethod} {RequestPath} completed with {StatusCode} in {ElapsedMilliseconds} ms ({TraceId})",
                method,
                path,
                statusCode,
                sw.ElapsedMilliseconds,
                traceId);

            if (!context.Request.Path.StartsWithSegments("/hubs", StringComparison.OrdinalIgnoreCase))
            {
                await operationalActivityNotifier.NotifyAsync(new OperationalActivityEvent(
                    DateTimeOffset.UtcNow,
                    statusCode >= 500 ? "error" : statusCode >= 400 ? "warn" : "info",
                    "http",
                    $"{method} {path} -> {statusCode} in {sw.ElapsedMilliseconds} ms",
                    traceId,
                    context.User.FindFirstValue("sub") ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier),
                    statusCode,
                    sw.ElapsedMilliseconds),
                    context.RequestAborted);
            }
        }
    }
}

using System.Net;
using System.Text.Json;
using algo.Domain.Logging.Entities;
using algo.Persistence.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;

namespace algo.API.Middleware;

public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger,
    IHostEnvironment environment)
{
    private static readonly HashSet<string> SensitiveKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "password",
        "newPassword",
        "confirmPassword",
        "token",
        "access_token",
        "refresh_token",
        "id_token",
        "otp",
        "otpCode",
        "authorization",
        "cookie",
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public async Task InvokeAsync(HttpContext context, ApplicationDbContext dbContext)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex) when (!context.Response.HasStarted)
        {
            var statusCode = StatusCodes.Status500InternalServerError;

            logger.LogError(
                ex,
                "Unhandled exception {ExceptionType} at {RequestPath} ({TraceId})",
                ex.GetType().FullName,
                context.Request.Path,
                context.TraceIdentifier);

            var errorLog = BuildErrorLog(context, ex, environment, statusCode);
            await PersistErrorLogAsync(dbContext, errorLog, context.RequestAborted);

            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/problem+json";

            var problem = new ProblemDetails
            {
                Status = context.Response.StatusCode,
                Title = "An unexpected error occurred.",
                Type = "https://httpstatuses.com/500",
                Instance = context.Request.Path.Value,
            };

            problem.Detail = environment.IsDevelopment()
                ? ex.Message
                : "An error occurred while processing your request.";

            await context.Response.WriteAsync(JsonSerializer.Serialize(problem, JsonOptions));
        }
    }

    private async Task PersistErrorLogAsync(
        ApplicationDbContext dbContext,
        ErrorLog errorLog,
        CancellationToken cancellationToken)
    {
        try
        {
            dbContext.ErrorLogs.Add(errorLog);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception persistenceEx)
        {
            logger.LogError(persistenceEx, "Failed to persist error log for trace {TraceId}", errorLog.TraceId);
        }
    }

    private static ErrorLog BuildErrorLog(
        HttpContext context,
        Exception exception,
        IHostEnvironment hostEnvironment,
        int statusCode)
    {
        var userId = context.User.FindFirst("sub")?.Value
            ?? context.User.FindFirst("userId")?.Value
            ?? context.User.FindFirst("uid")?.Value;
        var userName = context.User.Identity?.Name;

        return new ErrorLog
        {
            Timestamp = DateTimeOffset.UtcNow,
            Level = "Error",
            ExceptionType = exception.GetType().FullName ?? exception.GetType().Name,
            Message = exception.Message,
            StackTrace = exception.StackTrace,
            Source = exception.Source,
            Path = context.Request.Path.Value,
            Method = context.Request.Method,
            StatusCode = statusCode,
            TraceId = context.TraceIdentifier,
            UserId = userId,
            UserName = userName,
            RequestBody = null,
            QueryString = SanitizeQueryString(context.Request.Query),
            Headers = SerializeSafeHeaders(context.Request.Headers),
            Environment = hostEnvironment.EnvironmentName,
            MachineName = Environment.MachineName,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    private static string? SanitizeQueryString(IQueryCollection query)
    {
        if (query.Count == 0)
        {
            return null;
        }

        var sanitized = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in query)
        {
            var key = entry.Key;
            var value = SensitiveKeys.Contains(key) ? "***REDACTED***" : string.Join(",", entry.Value.ToArray());
            sanitized[key] = value;
        }

        return JsonSerializer.Serialize(sanitized, JsonOptions);
    }

    private static string? SerializeSafeHeaders(IHeaderDictionary headers)
    {
        var safeHeaders = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        foreach (var header in headers)
        {
            if (header.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase) ||
                header.Key.Equals("Cookie", StringComparison.OrdinalIgnoreCase) ||
                header.Key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var value = SensitiveKeys.Contains(header.Key) ? "***REDACTED***" : string.Join(",", header.Value.ToArray());
            safeHeaders[header.Key] = value;
        }

        if (safeHeaders.Count == 0)
        {
            return null;
        }

        return JsonSerializer.Serialize(safeHeaders, JsonOptions);
    }
}

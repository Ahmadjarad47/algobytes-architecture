using System.Security.Claims;
using algo.SharedKernel.Constants;
using Microsoft.AspNetCore.Http;
using Serilog.Core;
using Serilog.Events;


namespace algo.Infrastructure.Logging;

public sealed class LoggingEnricher(IHttpContextAccessor httpContextAccessor) : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var http = httpContextAccessor.HttpContext;
        if (http is null)
        {
            return;
        }

        var traceId = System.Diagnostics.Activity.Current?.Id ?? http.TraceIdentifier;
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(LoggingConstants.TraceId, traceId));

        if (http.Request.Path.Value is { Length: > 0 } path)
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(LoggingConstants.RequestPath, path));
        }

        logEvent.AddPropertyIfAbsent(
            propertyFactory.CreateProperty(LoggingConstants.RequestMethod, http.Request.Method));

        var principal = http.User;
        if (principal?.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(LoggingConstants.UserId, userId));
        }

        var userName = principal.Identity?.Name
            ?? principal.FindFirst(ClaimTypes.Email)?.Value
            ?? principal.FindFirst("preferred_username")?.Value;

        if (!string.IsNullOrEmpty(userName))
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(LoggingConstants.UserName, userName));
        }
    }
}

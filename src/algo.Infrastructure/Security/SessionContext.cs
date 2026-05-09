using algo.Application.Abstractions;
using Microsoft.AspNetCore.Http;

namespace algo.Infrastructure.Security;

public sealed class SessionContext(IHttpContextAccessor httpContextAccessor) : ISessionContext
{
    private string UserAgentValue =>
        httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString() ?? string.Empty;

    public string? IpAddress =>
        httpContextAccessor.HttpContext?.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim()
        ?? httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public string? UserAgent => string.IsNullOrWhiteSpace(UserAgentValue) ? null : UserAgentValue;

    public string? Location => null;

    public string Device => ParseDevice(UserAgentValue);

    public string Browser => ParseBrowser(UserAgentValue);

    public string OperatingSystem => ParseOperatingSystem(UserAgentValue);

    public bool IsSuspicious => string.IsNullOrWhiteSpace(UserAgentValue) || Browser == "Unknown";

    private static string ParseDevice(string userAgent)
    {
        if (Contains(userAgent, "Mobile", "Android", "iPhone"))
        {
            return "Mobile";
        }

        if (Contains(userAgent, "iPad", "Tablet"))
        {
            return "Tablet";
        }

        return "Desktop";
    }

    private static string ParseBrowser(string userAgent)
    {
        if (Contains(userAgent, "Edg/"))
        {
            return "Edge";
        }

        if (Contains(userAgent, "Chrome/") && !Contains(userAgent, "Edg/"))
        {
            return "Chrome";
        }

        if (Contains(userAgent, "Firefox/"))
        {
            return "Firefox";
        }

        if (Contains(userAgent, "Safari/") && !Contains(userAgent, "Chrome/"))
        {
            return "Safari";
        }

        return "Unknown";
    }

    private static string ParseOperatingSystem(string userAgent)
    {
        if (Contains(userAgent, "Windows"))
        {
            return "Windows";
        }

        if (Contains(userAgent, "Mac OS", "Macintosh"))
        {
            return "macOS";
        }

        if (Contains(userAgent, "Android"))
        {
            return "Android";
        }

        if (Contains(userAgent, "iPhone", "iPad"))
        {
            return "iOS";
        }

        if (Contains(userAgent, "Linux"))
        {
            return "Linux";
        }

        return "Unknown";
    }

    private static bool Contains(string value, params string[] needles) =>
        needles.Any(needle => value.Contains(needle, StringComparison.OrdinalIgnoreCase));
}

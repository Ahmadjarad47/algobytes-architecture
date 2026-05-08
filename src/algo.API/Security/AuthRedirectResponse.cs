using System.Text.Json;

namespace algo.API.Security;

internal static class AuthRedirectResponse
{
    private const string LoginPath = "/login";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static string BuildLoginRedirectUrl(HttpContext context)
    {
        var returnUrl = context.Request.PathBase + context.Request.Path + context.Request.QueryString;
        return string.IsNullOrWhiteSpace(returnUrl)
            ? LoginPath
            : $"{LoginPath}?redirectUrl={Uri.EscapeDataString(returnUrl)}";
    }

    public static async Task WriteAsync(
        HttpContext context,
        int statusCode,
        string error,
        string message)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var body = new
        {
            error,
            message,
            redirectUrl = BuildLoginRedirectUrl(context),
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(body, JsonOptions));
    }
}

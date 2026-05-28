namespace algo.API.Security;

internal static class AuthRedirectResponse
{
    private const string LoginPath = "/login";

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

        var problem = ProblemDetailsResponse.Create(context, statusCode, error, message);
        problem.Extensions["redirectUrl"] = BuildLoginRedirectUrl(context);

        await ProblemDetailsResponse.WriteAsync(context, problem, context.RequestAborted);
    }
}

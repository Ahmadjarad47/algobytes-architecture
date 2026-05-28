using algo.API.Security;

namespace algo.API.Extensions;

public static class ProblemDetailsApplicationBuilderExtensions
{
    private static readonly HashSet<int> ProblemStatusCodes =
    [
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status404NotFound,
        StatusCodes.Status409Conflict,
        StatusCodes.Status429TooManyRequests,
    ];

    public static IApplicationBuilder UseApiProblemDetails(this IApplicationBuilder app) =>
        app.UseStatusCodePages(async statusCodeContext =>
        {
            var httpContext = statusCodeContext.HttpContext;
            if (!ProblemStatusCodes.Contains(httpContext.Response.StatusCode) ||
                httpContext.Response.HasStarted ||
                httpContext.Response.ContentLength > 0)
            {
                return;
            }

            var problem = ProblemDetailsResponse.Create(
                httpContext,
                httpContext.Response.StatusCode,
                GetTitle(httpContext.Response.StatusCode));

            await ProblemDetailsResponse.WriteAsync(httpContext, problem, httpContext.RequestAborted);
        });

    private static string GetTitle(int statusCode) =>
        statusCode switch
        {
            StatusCodes.Status400BadRequest => "Bad request.",
            StatusCodes.Status401Unauthorized => "Unauthorized.",
            StatusCodes.Status403Forbidden => "Forbidden.",
            StatusCodes.Status404NotFound => "Not found.",
            StatusCodes.Status409Conflict => "Conflict.",
            StatusCodes.Status429TooManyRequests => "Too many requests.",
            _ => "Request failed.",
        };
}

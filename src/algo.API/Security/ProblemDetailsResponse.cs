using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace algo.API.Security;

internal static class ProblemDetailsResponse
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static ProblemDetails Create(
        HttpContext context,
        int statusCode,
        string title,
        string? detail = null,
        string? type = null)
    {
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Type = type ?? $"https://httpstatuses.com/{statusCode}",
            Instance = context.Request.Path.Value,
        };

        problem.Extensions["traceId"] = context.TraceIdentifier;
        return problem;
    }

    public static ValidationProblemDetails CreateValidation(
        HttpContext context,
        IDictionary<string, string[]> errors,
        int statusCode,
        string title,
        string? detail = null)
    {
        var problem = new ValidationProblemDetails(errors)
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Type = $"https://httpstatuses.com/{statusCode}",
            Instance = context.Request.Path.Value,
        };

        problem.Extensions["traceId"] = context.TraceIdentifier;
        return problem;
    }

    public static Task WriteAsync(HttpContext context, ProblemDetails problem, CancellationToken cancellationToken = default)
    {
        context.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";
        return JsonSerializer.SerializeAsync(context.Response.Body, problem, JsonOptions, cancellationToken);
    }
}

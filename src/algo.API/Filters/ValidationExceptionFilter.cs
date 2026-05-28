using algo.API.Security;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace algo.API.Filters;

public sealed class ValidationExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not ValidationException vex)
            return;

        var errors = vex.Errors
            .GroupBy(e => string.IsNullOrEmpty(e.PropertyName) ? "_error" : e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

        var hasAuthorizationError = errors.ContainsKey("authorization");
        var hasAuthenticationError = errors.ContainsKey("authentication");
        if (hasAuthenticationError || hasAuthorizationError)
        {
            var statusCode = hasAuthenticationError
                ? StatusCodes.Status401Unauthorized
                : StatusCodes.Status403Forbidden;

            var problem = ProblemDetailsResponse.CreateValidation(
                context.HttpContext,
                errors,
                statusCode,
                hasAuthenticationError ? "Unauthorized." : "Forbidden.");
            problem.Extensions["redirectUrl"] = AuthRedirectResponse.BuildLoginRedirectUrl(context.HttpContext);

            context.Result = new ObjectResult(problem)
            {
                StatusCode = statusCode,
            };
        }
        else
        {
            var problem = ProblemDetailsResponse.CreateValidation(
                context.HttpContext,
                errors,
                StatusCodes.Status400BadRequest,
                "Validation failed.");

            context.Result = new BadRequestObjectResult(problem);
        }

        context.ExceptionHandled = true;
    }
}

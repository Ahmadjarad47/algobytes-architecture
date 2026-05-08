using algo.Application.Abstractions;
using FluentValidation;
using FluentValidation.Results;

namespace algo.Application.Common.AccessPolicy;

public static class AccessPolicyAuthorizationExtensions
{
    public static async Task EnsureResourceActionAllowedAsync(
        this IAccessPolicyEvaluator evaluator,
        IApplicationDbContext db,
        string resource,
        string action,
        CancellationToken cancellationToken = default)
    {
        _ = db;
        if (!await evaluator.IsAllowedAsync(resource, action, cancellationToken))
        {
            throw BuildForbiddenValidationException(resource, action);
        }
    }

    public static ValidationException BuildForbiddenValidationException(string resource, string action) =>
        new(new[]
        {
            new ValidationFailure("authorization", $"Forbidden: {resource}:{action}."),
        });
}

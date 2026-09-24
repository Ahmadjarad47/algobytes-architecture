namespace algo.Application.Abstractions.Services;

public interface IAccessPolicyAuthorizationChecker
{
    Task<bool> IsAllowedAsync(
        string resource,
        string action,
        CancellationToken cancellationToken = default);
}


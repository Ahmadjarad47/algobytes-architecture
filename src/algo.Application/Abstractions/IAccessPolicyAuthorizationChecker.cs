namespace algo.Application.Abstractions;

public interface IAccessPolicyAuthorizationChecker
{
    Task<bool> IsAllowedAsync(
        string resource,
        string action,
        CancellationToken cancellationToken = default);
}

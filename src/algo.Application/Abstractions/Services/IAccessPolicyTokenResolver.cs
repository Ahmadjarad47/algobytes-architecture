namespace algo.Application.Abstractions.Services;

public interface IAccessPolicyTokenResolver
{
    string? CurrentUserId { get; }

    IReadOnlyList<string> CurrentRoleNames { get; }

    object? ResolveTokenValue(string token);
}


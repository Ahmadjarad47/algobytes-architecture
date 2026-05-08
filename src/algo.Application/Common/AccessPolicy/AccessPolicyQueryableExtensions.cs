using algo.Application.Abstractions;

namespace algo.Application.Common.AccessPolicy;

public static class AccessPolicyQueryableExtensions
{
    public static Task<IQueryable<TEntity>> ApplyAccessPolicyAsync<TEntity>(
        this IQueryable<TEntity> query,
        IAccessPolicyEvaluator evaluator,
        string resource,
        string action,
        CancellationToken cancellationToken = default)
        where TEntity : class =>
        evaluator.ApplyAsync(query, resource, action, cancellationToken);
}

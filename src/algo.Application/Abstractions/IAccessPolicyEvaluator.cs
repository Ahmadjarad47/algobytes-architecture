namespace algo.Application.Abstractions;

public interface IAccessPolicyEvaluator
{
    Task<bool> IsAllowedAsync(
        string resource,
        string action,
        CancellationToken cancellationToken = default);

    Task<IQueryable<TEntity>> ApplyAsync<TEntity>(
        IQueryable<TEntity> query,
        string resource,
        string action,
        CancellationToken cancellationToken = default)
        where TEntity : class;
}

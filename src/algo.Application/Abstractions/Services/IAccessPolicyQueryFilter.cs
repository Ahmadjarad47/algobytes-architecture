namespace algo.Application.Abstractions;

public interface IAccessPolicyQueryFilter
{
    Task<IQueryable<TEntity>> ApplyAsync<TEntity>(
        IQueryable<TEntity> query,
        string resource,
        string action,
        CancellationToken cancellationToken = default)
        where TEntity : class;
}

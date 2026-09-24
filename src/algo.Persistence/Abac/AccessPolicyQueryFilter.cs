using System.Linq.Expressions;
using algo.Application.Abstractions;
using algo.Application.Common.AccessPolicy;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;

namespace algo.Persistence.Abac;

public sealed class AccessPolicyQueryFilter(
    IAccessPolicyRuleStore ruleStore,
    IAccessPolicyConditionParser conditionParser,
    IAccessPolicyTokenResolver tokenResolver,
    IAccessPolicyMetadataLookup metadataLookup,
    AccessPolicyExpressionCompiler compiler,
    ILogger<AccessPolicyQueryFilter> logger) : IAccessPolicyQueryFilter
{
    public async Task<IQueryable<TEntity>> ApplyAsync<TEntity>(
        IQueryable<TEntity> query,
        string resource,
        string action,
        CancellationToken cancellationToken = default)
        where TEntity : class
    {
        if (!metadataLookup.TryGetMetadata(resource, out var metadata) || metadata is null)
        {
            throw new InvalidOperationException($"Unknown resource '{resource}' for access policy evaluation.");
        }

        if (metadata.EntityType != typeof(TEntity))
        {
            throw new InvalidOperationException(
                $"Resource '{resource}' is not mapped to entity {typeof(TEntity).Name}.");
        }

        logger.LogInformation(
            "Access policy query filter started for {Resource}:{Action}",
            resource,
            action);

        var rules = await ruleStore.GetActiveRulesAsync(cancellationToken);
        var userId = tokenResolver.CurrentUserId;
        var roles = tokenResolver.CurrentRoleNames;

        var matched = AccessPolicyRuleMatcher.MatchRules(rules, resource, action, userId, roles);

        logger.LogInformation(
            "Access policy query filter for {Resource}:{Action}. UserId={UserId}, Roles=[{Roles}], ActiveRules={ActiveRuleCount}, SubjectMatched={SubjectMatchedCount}, ResourceActionMatched={MatchedCount}, Allows={AllowCount}, Denies={DenyCount}",
            resource,
            action,
            userId ?? "<anonymous>",
            string.Join(", ", roles),
            matched.ActiveRuleCount,
            matched.SubjectMatchedCount,
            matched.ResourceActionMatchedCount,
            matched.Allows.Count,
            matched.Denies.Count);

        if (matched.Allows.Count == 0)
        {
            return query.Where(_ => false);
        }

        var fullAllow = matched.Allows.Any(a => string.IsNullOrWhiteSpace(a.ConditionJson));
        var denyAll = matched.Denies.Any(d => string.IsNullOrWhiteSpace(d.ConditionJson));

        Expression<Func<TEntity, bool>>? allowExpr = null;
        if (!fullAllow)
        {
            foreach (var allow in matched.Allows)
            {
                var ast = conditionParser.Parse(allow.ConditionJson);
                conditionParser.Validate(resource, ast, metadataLookup);
                ast = AccessPolicyConditionTokenResolver.Resolve(ast, tokenResolver);
                var compiled = compiler.Compile<TEntity>(ast, metadata);
                allowExpr = allowExpr is null ? compiled : Or(allowExpr, compiled);
            }
        }

        Expression<Func<TEntity, bool>>? denyExpr = null;
        if (denyAll)
        {
            denyExpr = _ => true;
        }
        else
        {
            foreach (var deny in matched.Denies.Where(d => !string.IsNullOrWhiteSpace(d.ConditionJson)))
            {
                var ast = conditionParser.Parse(deny.ConditionJson);
                conditionParser.Validate(resource, ast, metadataLookup);
                ast = AccessPolicyConditionTokenResolver.Resolve(ast, tokenResolver);
                var compiled = compiler.Compile<TEntity>(ast, metadata);
                denyExpr = denyExpr is null ? compiled : Or(denyExpr, compiled);
            }
        }

        Expression<Func<TEntity, bool>> predicate;
        if (fullAllow && denyExpr is null)
        {
            predicate = _ => true;
        }
        else if (fullAllow && denyExpr is not null)
        {
            var param = Expression.Parameter(typeof(TEntity), "e");
            var denyBody = ReplacingExpressionVisitor.Replace(
                denyExpr.Parameters.Single(),
                param,
                denyExpr.Body);
            var notDeny = Expression.Not(denyBody);
            predicate = Expression.Lambda<Func<TEntity, bool>>(notDeny, param);
        }
        else if (!fullAllow && allowExpr is not null && denyExpr is null)
        {
            predicate = allowExpr;
        }
        else if (!fullAllow && allowExpr is not null && denyExpr is not null)
        {
            predicate = AndNot(allowExpr, denyExpr);
        }
        else
        {
            predicate = _ => false;
        }

        return query.Where(predicate);
    }

    private static Expression<Func<T, bool>> Or<T>(
        Expression<Func<T, bool>> left,
        Expression<Func<T, bool>> right)
    {
        var param = Expression.Parameter(typeof(T), "e");
        var leftBody = ReplacingExpressionVisitor.Replace(left.Parameters.Single(), param, left.Body);
        var rightBody = ReplacingExpressionVisitor.Replace(right.Parameters.Single(), param, right.Body);
        return Expression.Lambda<Func<T, bool>>(Expression.OrElse(leftBody, rightBody), param);
    }

    private static Expression<Func<T, bool>> AndNot<T>(
        Expression<Func<T, bool>> allow,
        Expression<Func<T, bool>> deny)
    {
        var param = Expression.Parameter(typeof(T), "e");
        var allowBody = ReplacingExpressionVisitor.Replace(allow.Parameters.Single(), param, allow.Body);
        var denyBody = ReplacingExpressionVisitor.Replace(deny.Parameters.Single(), param, deny.Body);
        var body = Expression.AndAlso(allowBody, Expression.Not(denyBody));
        return Expression.Lambda<Func<T, bool>>(body, param);
    }
}

using System.Linq.Expressions;
using algo.Application.Abstractions;
using algo.Application.Common.AccessPolicy;
using algo.Domain.Identity.Policies;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace algo.Persistence.Abac;

public sealed class AccessPolicyEvaluator(
    IAccessPolicyRuleStore ruleStore,
    IAccessPolicyConditionParser conditionParser,
    IAccessPolicyTokenResolver tokenResolver,
    IAccessPolicyMetadataProvider metadataProvider,
    AccessPolicyExpressionCompiler compiler,
    ILogger<AccessPolicyEvaluator> logger) : IAccessPolicyEvaluator
{
    public async Task<bool> IsAllowedAsync(
        string resource,
        string action,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Access policy check started for {Resource}:{Action}",
            resource,
            action);

        var rules = await ruleStore.GetActiveRulesAsync(cancellationToken);
        var userId = tokenResolver.CurrentUserId;
        var roles = tokenResolver.CurrentRoleNames;

        var subjectMatched = rules
            .Where(r => MatchesSubject(r, userId, roles))
            .ToList();

        var matched = subjectMatched
            .Where(r => MatchesResource(r.Resource, resource))
            .Where(r => MatchesAction(r.Action, action))
            .ToList();

        var allows = matched.Where(r => r.Effect == AccessPolicyEffect.Allow).ToList();
        var denies = matched.Where(r => r.Effect == AccessPolicyEffect.Deny).ToList();

        var allowed = ComputeDecision(allows, denies);

        logger.LogInformation(
            "Access policy evaluation for {Resource}:{Action}. UserId={UserId}, Roles=[{Roles}], ActiveRules={ActiveRuleCount}, SubjectMatched={SubjectMatchedCount}, ResourceActionMatched={MatchedCount}, Allows={AllowCount}, Denies={DenyCount}, Decision={Decision}",
            resource,
            action,
            userId ?? "<anonymous>",
            string.Join(", ", roles),
            rules.Count,
            subjectMatched.Count,
            matched.Count,
            allows.Count,
            denies.Count,
            allowed ? "Allowed" : "Denied");

        return allowed;
    }

    private static bool ComputeDecision(
        IReadOnlyList<AccessPolicyRuleDto> allows,
        IReadOnlyList<AccessPolicyRuleDto> denies)
    {
        if (allows.Count == 0)
        {
            return false;
        }

        var denyAll = denies.Any(d => string.IsNullOrWhiteSpace(d.ConditionJson));
        if (denyAll)
        {
            return false;
        }

        return allows.Any();
    }

    public async Task<IQueryable<TEntity>> ApplyAsync<TEntity>(
        IQueryable<TEntity> query,
        string resource,
        string action,
        CancellationToken cancellationToken = default)
        where TEntity : class
    {
        if (!metadataProvider.TryGetMetadata(resource, out var metadata) || metadata is null)
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

        var subjectMatched = rules
            .Where(r => MatchesSubject(r, userId, roles))
            .ToList();

        var matched = subjectMatched
            .Where(r => MatchesResource(r.Resource, resource))
            .Where(r => MatchesAction(r.Action, action))
            .ToList();

        var allows = matched.Where(r => r.Effect == AccessPolicyEffect.Allow).ToList();
        var denies = matched.Where(r => r.Effect == AccessPolicyEffect.Deny).ToList();

        logger.LogInformation(
            "Access policy query filter for {Resource}:{Action}. UserId={UserId}, Roles=[{Roles}], ActiveRules={ActiveRuleCount}, SubjectMatched={SubjectMatchedCount}, ResourceActionMatched={MatchedCount}, Allows={AllowCount}, Denies={DenyCount}",
            resource,
            action,
            userId ?? "<anonymous>",
            string.Join(", ", roles),
            rules.Count,
            subjectMatched.Count,
            matched.Count,
            allows.Count,
            denies.Count);

        if (allows.Count == 0)
        {
            return query.Where(_ => false);
        }

        var fullAllow = allows.Any(a => string.IsNullOrWhiteSpace(a.ConditionJson));
        var denyAll = denies.Any(d => string.IsNullOrWhiteSpace(d.ConditionJson));

        Expression<Func<TEntity, bool>>? allowExpr = null;
        if (!fullAllow)
        {
            foreach (var allow in allows)
            {
                var ast = conditionParser.Parse(allow.ConditionJson);
                conditionParser.Validate(resource, ast, metadataProvider);
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
            foreach (var deny in denies.Where(d => !string.IsNullOrWhiteSpace(d.ConditionJson)))
            {
                var ast = conditionParser.Parse(deny.ConditionJson);
                conditionParser.Validate(resource, ast, metadataProvider);
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

    private static bool MatchesResource(string policyResource, string resource) =>
        string.Equals(policyResource, AccessPolicyResources.Wildcard, StringComparison.Ordinal)
        || string.Equals(policyResource, resource, StringComparison.OrdinalIgnoreCase);

    private static bool MatchesAction(string policyAction, string action) =>
        string.Equals(policyAction, AccessPolicyActions.Wildcard, StringComparison.Ordinal)
        || string.Equals(policyAction, action, StringComparison.OrdinalIgnoreCase);

    private static bool MatchesSubject(AccessPolicyRuleDto rule, string? userId, IReadOnlyList<string> roles) =>
        rule.SubjectType switch
        {
            AccessPolicySubjectType.Everyone => true,
            AccessPolicySubjectType.Authenticated => !string.IsNullOrEmpty(userId),
            AccessPolicySubjectType.User => !string.IsNullOrEmpty(userId)
                && string.Equals(rule.SubjectKey, userId, StringComparison.Ordinal),
            AccessPolicySubjectType.Role => roles.Any(r =>
                string.Equals(r, rule.SubjectKey, StringComparison.OrdinalIgnoreCase)),
            _ => false,
        };

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

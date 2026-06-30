using algo.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace algo.Persistence.Abac;

public sealed class AccessPolicyAuthorizationChecker(
    IAccessPolicyRuleStore ruleStore,
    IAccessPolicyTokenResolver tokenResolver,
    ILogger<AccessPolicyAuthorizationChecker> logger) : IAccessPolicyAuthorizationChecker
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

        var matched = AccessPolicyRuleMatcher.MatchRules(rules, resource, action, userId, roles);
        var allowed = AccessPolicyRuleMatcher.ComputeDecision(matched.Allows, matched.Denies);

        logger.LogInformation(
            "Access policy evaluation for {Resource}:{Action}. UserId={UserId}, Roles=[{Roles}], ActiveRules={ActiveRuleCount}, SubjectMatched={SubjectMatchedCount}, ResourceActionMatched={MatchedCount}, Allows={AllowCount}, Denies={DenyCount}, Decision={Decision}",
            resource,
            action,
            userId ?? "<anonymous>",
            string.Join(", ", roles),
            matched.ActiveRuleCount,
            matched.SubjectMatchedCount,
            matched.ResourceActionMatchedCount,
            matched.Allows.Count,
            matched.Denies.Count,
            allowed ? "Allowed" : "Denied");

        return allowed;
    }
}

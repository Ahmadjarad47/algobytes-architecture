using algo.Application.Common.AccessPolicy;
using algo.Domain.Identity.Policies;

namespace algo.Persistence.Abac;

internal static class AccessPolicyRuleMatcher
{
    internal sealed record MatchedRules(
        IReadOnlyList<AccessPolicyRuleDto> Allows,
        IReadOnlyList<AccessPolicyRuleDto> Denies,
        int ActiveRuleCount,
        int SubjectMatchedCount,
        int ResourceActionMatchedCount);

    public static MatchedRules MatchRules(
        IReadOnlyList<AccessPolicyRuleDto> rules,
        string resource,
        string action,
        string? userId,
        IReadOnlyList<string> roles)
    {
        var subjectMatched = rules
            .Where(r => MatchesSubject(r, userId, roles))
            .ToList();

        var matched = subjectMatched
            .Where(r => MatchesResource(r.Resource, resource))
            .Where(r => MatchesAction(r.Action, action))
            .ToList();

        var allows = matched.Where(r => r.Effect == AccessPolicyEffect.Allow).ToList();
        var denies = matched.Where(r => r.Effect == AccessPolicyEffect.Deny).ToList();

        return new MatchedRules(allows, denies, rules.Count, subjectMatched.Count, matched.Count);
    }

    public static bool ComputeDecision(
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
}

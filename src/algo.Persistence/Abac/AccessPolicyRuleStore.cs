using algo.Application.Abstractions;
using algo.Application.Common.AccessPolicy;
using algo.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace algo.Persistence.Abac;

public sealed class AccessPolicyRuleStore(ApplicationDbContext db) : IAccessPolicyRuleStore
{
    public async Task<IReadOnlyList<AccessPolicyRuleDto>> GetActiveRulesAsync(
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        return await db.AccessPolicies
            .AsNoTracking()
            .Where(p => p.IsEnabled && p.DeletedAt == null
                && (p.ValidFrom == null || p.ValidFrom <= now)
                && (p.ValidTo == null || p.ValidTo >= now))
            .OrderBy(p => p.Priority)
            .ThenBy(p => p.Id)
            .Select(p => new AccessPolicyRuleDto(
                p.Id,
                p.Resource,
                p.Action,
                p.Effect,
                p.SubjectType,
                p.SubjectKey,
                p.ConditionJson,
                p.Priority))
            .ToListAsync(cancellationToken);
    }
}

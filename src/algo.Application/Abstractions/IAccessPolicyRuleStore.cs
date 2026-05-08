using algo.Application.Common.AccessPolicy;

namespace algo.Application.Abstractions;

public interface IAccessPolicyRuleStore
{
    Task<IReadOnlyList<AccessPolicyRuleDto>> GetActiveRulesAsync(CancellationToken cancellationToken = default);
}

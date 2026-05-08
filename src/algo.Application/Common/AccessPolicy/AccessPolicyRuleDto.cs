using algo.Domain.Identity.Policies;

namespace algo.Application.Common.AccessPolicy;

public sealed record AccessPolicyRuleDto(
    Guid Id,
    string Resource,
    string Action,
    AccessPolicyEffect Effect,
    AccessPolicySubjectType SubjectType,
    string SubjectKey,
    string? ConditionJson,
    int Priority);

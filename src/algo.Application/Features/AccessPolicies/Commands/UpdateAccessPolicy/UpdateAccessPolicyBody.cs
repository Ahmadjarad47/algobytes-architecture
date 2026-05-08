using algo.Domain.Identity.Policies;

namespace algo.Application.Features.AccessPolicies.Commands.UpdateAccessPolicy;

public sealed record UpdateAccessPolicyBody(
    string Resource,
    string Action,
    AccessPolicyEffect Effect,
    AccessPolicySubjectType SubjectType,
    string SubjectKey,
    string? ConditionJson,
    int Priority,
    bool IsEnabled,
    string? Description,
    DateTime? ValidFrom,
    DateTime? ValidTo);

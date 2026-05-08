using algo.Domain.Identity.Policies;

namespace algo.Application.Features.AccessPolicies.Dtos;

public sealed record AccessPolicyAdminDto(
    Guid Id,
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
    DateTime? ValidTo,
    DateTime? DeletedAt,
    string? CreatedByUserId,
    string? UpdatedByUserId);

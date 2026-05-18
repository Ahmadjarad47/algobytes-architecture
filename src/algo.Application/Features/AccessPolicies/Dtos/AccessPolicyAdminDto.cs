using algo.Domain.Identity.Policies;

using System.Text.Json;

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
    DateTime? TrashedAt,
    DateTime? TrashExpiresAt,
    DateTime? DeletedAt,
    JsonElement? CustomFields,
    string? CreatedByUserId,
    string? UpdatedByUserId);

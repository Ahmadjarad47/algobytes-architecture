using System.Text.Json;
using algo.Application.Features.AccessPolicies.Dtos;
using algo.Domain.Identity.Policies;
using MediatR;

namespace algo.Application.Features.AccessPolicies.Commands.UpdateAccessPolicy;

public sealed record UpdateAccessPolicyCommand(
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
    JsonElement? CustomFields) : IRequest<AccessPolicyAdminDto?>;

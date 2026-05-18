using System.Text.Json;
using algo.Application.Features.AccessPolicies.Dtos;
using algo.Domain.Identity.Policies;
using MediatR;

namespace algo.Application.Features.AccessPolicies.Commands.CreateAccessPolicy;

public sealed record CreateAccessPolicyCommand(
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
    JsonElement? CustomFields) : IRequest<AccessPolicyAdminDto>;

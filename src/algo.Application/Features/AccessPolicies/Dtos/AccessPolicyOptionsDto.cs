using algo.Domain.Identity.Policies;

namespace algo.Application.Features.AccessPolicies.Dtos;

public sealed record AccessPolicyOptionsDto(
    IReadOnlyList<string> Resources,
    IReadOnlyDictionary<string, IReadOnlyList<string>> ActionsByResource,
    IReadOnlyList<AccessPolicyEnumOptionDto<AccessPolicyEffect>> Effects,
    IReadOnlyList<AccessPolicyEnumOptionDto<AccessPolicySubjectType>> SubjectTypes,
    IReadOnlyDictionary<string, IReadOnlyList<AccessPolicyConditionFieldOptionDto>> ConditionFieldsByResource);

public sealed record AccessPolicyEnumOptionDto<TEnum>(TEnum Value, string Label) where TEnum : struct, Enum;

public sealed record AccessPolicyConditionFieldOptionDto(
    string Field,
    string Label,
    string Type,
    IReadOnlyList<string> Operators,
    IReadOnlyList<AccessPolicyConditionEnumOptionDto>? Options = null);

public sealed record AccessPolicyConditionOperatorOptionDto(string Value, string Label);

public sealed record AccessPolicyResourceOptionsDto(
    string Resource,
    IReadOnlyList<string> Actions,
    IReadOnlyList<AccessPolicyConditionFieldOptionDto> ConditionFields);

public sealed record AccessPolicyConditionEnumOptionDto(object Value, string Label);

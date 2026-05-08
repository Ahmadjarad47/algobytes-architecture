using algo.Application.Abstractions;
using algo.Application.Common.AccessPolicy;
using algo.Application.Features.AccessPolicies.Dtos;
using algo.Domain.Identity.Policies;
using MediatR;

namespace algo.Application.Features.AccessPolicies.Queries.GetAccessPolicyOptions;

public sealed class GetAccessPolicyOptionsQueryHandler(
    IApplicationDbContext db,
    IAccessPolicyEvaluator accessPolicyEvaluator,
    IAccessPolicyMetadataProvider metadataProvider)
    : IRequestHandler<GetAccessPolicyOptionsQuery, AccessPolicyOptionsDto>
{
    private static readonly IReadOnlyList<string> DefaultResourceActions =
        [AccessPolicyActions.Read, AccessPolicyActions.Create, AccessPolicyActions.Update, AccessPolicyActions.Delete];

    private static readonly IReadOnlyList<string> WildcardActions = [AccessPolicyActions.Wildcard];

    private static readonly IReadOnlyList<string> ComparableOperators = ["eq", "neq", "gt", "gte", "lt", "lte"];

    private static readonly IReadOnlyList<string> StringOperators =
        ["eq", "neq", "contains", "startsWith", "endsWith", "in", "nin", "isNull", "notNull"];

    private static readonly IReadOnlyList<string> EqualityOperators = ["eq", "neq"];

    private static readonly IReadOnlyList<string> SetOperators = ["eq", "neq", "in", "nin"];

    private static readonly IReadOnlyList<AccessPolicyEnumOptionDto<AccessPolicyEffect>> EffectOptions =
        Enum.GetValues<AccessPolicyEffect>()
            .Select(value => new AccessPolicyEnumOptionDto<AccessPolicyEffect>(value, value.ToString()))
            .ToArray();

    private static readonly IReadOnlyList<AccessPolicyEnumOptionDto<AccessPolicySubjectType>> SubjectTypeOptions =
        Enum.GetValues<AccessPolicySubjectType>()
            .Select(value => new AccessPolicyEnumOptionDto<AccessPolicySubjectType>(value, value.ToString()))
            .ToArray();

    public async Task<AccessPolicyOptionsDto> Handle(
        GetAccessPolicyOptionsQuery request,
        CancellationToken cancellationToken)
    {
        await accessPolicyEvaluator.EnsureResourceActionAllowedAsync(
            db,
            AccessPolicyResources.AccessPolicies,
            AccessPolicyActions.Read,
            cancellationToken);

        var resources = metadataProvider.GetRegisteredResources()
            .Order(StringComparer.OrdinalIgnoreCase)
            .Append(AccessPolicyResources.Wildcard)
            .ToArray();

        var actionsByResource = resources.ToDictionary(
            resource => resource,
            resource => string.Equals(resource, AccessPolicyResources.Wildcard, StringComparison.Ordinal)
                ? WildcardActions
                : DefaultResourceActions,
            StringComparer.OrdinalIgnoreCase);

        var conditionFieldsByResource = resources
            .ToDictionary(
                resource => resource,
                resource => !string.Equals(resource, AccessPolicyResources.Wildcard, StringComparison.Ordinal)
                            && metadataProvider.TryGetMetadata(resource, out var metadata)
                            && metadata is not null
                    ? metadata.Fields
                        .OrderBy(field => field.Key, StringComparer.OrdinalIgnoreCase)
                        .Select(field => ToConditionFieldOption(field.Key, field.Value.ClrType))
                        .ToArray()
                    : Array.Empty<AccessPolicyConditionFieldOptionDto>() as IReadOnlyList<AccessPolicyConditionFieldOptionDto>,
                StringComparer.OrdinalIgnoreCase);

        return new AccessPolicyOptionsDto(
            resources,
            actionsByResource,
            EffectOptions,
            SubjectTypeOptions,
            conditionFieldsByResource);
    }

    private static AccessPolicyConditionFieldOptionDto ToConditionFieldOption(string field, Type clrType)
    {
        var type = Nullable.GetUnderlyingType(clrType) ?? clrType;
        var conditionType = ToConditionType(type);

        return new AccessPolicyConditionFieldOptionDto(
            field,
            ToDisplayLabel(field),
            conditionType,
            OperatorsFor(conditionType),
            type.IsEnum ? EnumOptions(type) : null);
    }

    private static string ToConditionType(Type type)
    {
        if (type == typeof(string))
        {
            return "string";
        }

        if (type == typeof(bool))
        {
            return "boolean";
        }

        if (type == typeof(Guid))
        {
            return "guid";
        }

        if (type == typeof(DateTime) || type == typeof(DateTimeOffset))
        {
            return "date";
        }

        if (type.IsEnum)
        {
            return "enum";
        }

        return IsNumericType(type) ? "number" : "string";
    }

    private static IReadOnlyList<string> OperatorsFor(string conditionType) =>
        conditionType switch
        {
            "string" => StringOperators,
            "number" or "date" => ComparableOperators,
            "boolean" => EqualityOperators,
            "guid" or "enum" => SetOperators,
            _ => EqualityOperators,
        };

    private static IReadOnlyList<AccessPolicyConditionEnumOptionDto> EnumOptions(Type enumType) =>
        Enum.GetValues(enumType)
            .Cast<object>()
            .Select(value => new AccessPolicyConditionEnumOptionDto(Convert.ToInt32(value), value.ToString() ?? string.Empty))
            .ToArray();

    private static string ToDisplayLabel(string field)
    {
        var chars = field
            .SelectMany((ch, index) => index > 0 && char.IsUpper(ch) ? [' ', ch] : new[] { ch })
            .ToArray();

        var label = new string(chars).Replace('_', ' ').Replace('-', ' ');
        return char.ToUpperInvariant(label[0]) + label[1..];
    }

    private static bool IsNumericType(Type type) =>
        type == typeof(int) || type == typeof(long) || type == typeof(short)
        || type == typeof(byte) || type == typeof(float) || type == typeof(double) || type == typeof(decimal);
}

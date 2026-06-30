using System.Text.Json;
using algo.Application.Abstractions;
using algo.Application.Common.AccessPolicy;

namespace algo.Persistence.Abac;

public sealed class AccessPolicyConditionParser : IAccessPolicyConditionParser
{
    private static readonly HashSet<string> SupportedOperators = new(StringComparer.OrdinalIgnoreCase)
    {
        "eq", "neq", "gt", "gte", "lt", "lte",
        "in", "nin", "contains", "startsWith", "endsWith", "isNull", "notNull",
    };

    public AccessPolicyConditionAst Parse(string? conditionJson)
    {
        if (string.IsNullOrWhiteSpace(conditionJson))
        {
            throw new AccessPolicyConditionParseException("Condition JSON is empty.");
        }

        using var doc = JsonDocument.Parse(conditionJson);
        return ParseNode(doc.RootElement);
    }

    public void Validate(
        string resource,
        AccessPolicyConditionAst ast,
        IAccessPolicyMetadataLookup metadataLookup)
    {
        if (string.IsNullOrWhiteSpace(resource)
            || string.Equals(resource, AccessPolicyResources.Wildcard, StringComparison.Ordinal))
        {
            throw new AccessPolicyConditionValidationException(
                "A concrete resource is required to validate conditions.");
        }

        if (!metadataLookup.TryGetMetadata(resource, out var metadata) || metadata is null)
        {
            throw new AccessPolicyConditionValidationException($"Unknown resource '{resource}'.");
        }

        ValidateNode(ast, metadata);
    }

    private static AccessPolicyConditionAst ParseNode(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            throw new AccessPolicyConditionParseException("Condition must be a JSON object.");
        }

        var hasAll = element.TryGetProperty("all", out var allProp);
        var hasAny = element.TryGetProperty("any", out var anyProp);
        var hasField = element.TryGetProperty("field", out var fieldProp);

        var compositeCount = (hasAll ? 1 : 0) + (hasAny ? 1 : 0) + (hasField ? 1 : 0);
        if (compositeCount != 1)
        {
            throw new AccessPolicyConditionParseException(
                "Condition must contain exactly one of: 'all', 'any', or 'field'.");
        }

        if (hasAll)
        {
            if (allProp.ValueKind != JsonValueKind.Array)
            {
                throw new AccessPolicyConditionParseException("'all' must be an array.");
            }

            var items = allProp.EnumerateArray().Select(ParseNode).ToList();
            return new AccessPolicyAllAst(items);
        }

        if (hasAny)
        {
            if (anyProp.ValueKind != JsonValueKind.Array)
            {
                throw new AccessPolicyConditionParseException("'any' must be an array.");
            }

            var items = anyProp.EnumerateArray().Select(ParseNode).ToList();
            return new AccessPolicyAnyAst(items);
        }

        var field = fieldProp.GetString();
        if (string.IsNullOrWhiteSpace(field))
        {
            throw new AccessPolicyConditionParseException("'field' is required.");
        }

        if (!element.TryGetProperty("operator", out var opEl))
        {
            throw new AccessPolicyConditionParseException("'operator' is required.");
        }

        var op = opEl.GetString();
        if (string.IsNullOrWhiteSpace(op))
        {
            throw new AccessPolicyConditionParseException("'operator' is required.");
        }

        var opNorm = op.Trim().ToLowerInvariant();
        if (!SupportedOperators.Contains(opNorm))
        {
            throw new AccessPolicyConditionParseException($"Unsupported operator '{op}'.");
        }

        object? value = null;
        if (opNorm is "isNull" or "notNull")
        {
            if (element.TryGetProperty("value", out _))
            {
                throw new AccessPolicyConditionParseException(
                    $"'value' must not be provided for operator '{opNorm}'.");
            }
        }
        else if (!element.TryGetProperty("value", out var valueEl))
        {
            throw new AccessPolicyConditionParseException("'value' is required for this operator.");
        }
        else
        {
            value = ParseJsonValue(valueEl);
        }

        return new AccessPolicyFieldAst(field.Trim(), opNorm, value);
    }

    private static object? ParseJsonValue(JsonElement valueEl) =>
        valueEl.ValueKind switch
        {
            JsonValueKind.String => valueEl.GetString(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number => valueEl.TryGetInt64(out var l)
                ? l
                : valueEl.TryGetDouble(out var d)
                    ? d
                    : throw new AccessPolicyConditionParseException("Unsupported numeric value."),
            JsonValueKind.Array => valueEl.EnumerateArray().Select(ParseJsonValue).ToList<object?>(),
            JsonValueKind.Null => null,
            _ => throw new AccessPolicyConditionParseException("Unsupported JSON value kind."),
        };

    private static void ValidateNode(AccessPolicyConditionAst node, AccessPolicyEntityMetadata metadata)
    {
        switch (node)
        {
            case AccessPolicyAllAst all:
                foreach (var child in all.All)
                {
                    ValidateNode(child, metadata);
                }

                break;
            case AccessPolicyAnyAst any:
                foreach (var child in any.Any)
                {
                    ValidateNode(child, metadata);
                }

                break;
            case AccessPolicyFieldAst field:
                ValidateField(field, metadata);
                break;
            default:
                throw new AccessPolicyConditionValidationException("Unknown condition node.");
        }
    }

    private static void ValidateField(AccessPolicyFieldAst field, AccessPolicyEntityMetadata metadata)
    {
        if (!metadata.Fields.TryGetValue(field.Field, out var fieldMeta))
        {
            throw new AccessPolicyConditionValidationException(
                $"Unknown field '{field.Field}' for resource metadata.");
        }

        var op = field.Operator;

        if (op is "isNull" or "notNull")
        {
            return;
        }

        var value = field.Value;
        if (value is string s && s.StartsWith('@'))
        {
            if (s.Equals("@CurrentUserId", StringComparison.Ordinal)
                && fieldMeta.ClrType != typeof(string))
            {
                throw new AccessPolicyConditionValidationException(
                    $"Token @CurrentUserId is only valid for string fields (field '{field.Field}').");
            }

            if (s.Equals("@CurrentRoleNames", StringComparison.Ordinal)
                && op is not ("in" or "nin"))
            {
                throw new AccessPolicyConditionValidationException(
                    "Token @CurrentRoleNames is only valid with operators 'in' or 'nin'.");
            }

            return;
        }

        switch (op)
        {
            case "in":
            case "nin":
                if (value is string roleToken
                    && roleToken.Equals("@CurrentRoleNames", StringComparison.Ordinal))
                {
                    break;
                }

                if (value is not List<object?> list)
                {
                    throw new AccessPolicyConditionValidationException(
                        $"Operator '{op}' requires an array value.");
                }

                foreach (var item in list)
                {
                    if (item is string tokenStr && tokenStr.StartsWith('@'))
                    {
                        continue;
                    }

                    EnsureCoercible(item, fieldMeta.ClrType, field.Field, op);
                }

                break;
            default:
                EnsureCoercible(value, fieldMeta.ClrType, field.Field, op);
                break;
        }
    }

    private static void EnsureCoercible(object? value, Type targetType, string fieldName, string op)
    {
        if (value is null)
        {
            var acceptsNull = !targetType.IsValueType || Nullable.GetUnderlyingType(targetType) != null;
            if (!acceptsNull)
            {
                throw new AccessPolicyConditionValidationException(
                    $"Value for field '{fieldName}' cannot be null for operator '{op}'.");
            }

            return;
        }

        var nonNullable = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (nonNullable == typeof(string))
        {
            if (value is not string)
            {
                throw new AccessPolicyConditionValidationException(
                    $"Field '{fieldName}' expects a string value.");
            }

            return;
        }

        if (nonNullable == typeof(bool))
        {
            if (value is not bool)
            {
                throw new AccessPolicyConditionValidationException(
                    $"Field '{fieldName}' expects a boolean value.");
            }

            return;
        }

        if (nonNullable == typeof(DateTimeOffset))
        {
            switch (value)
            {
                case DateTimeOffset:
                    return;
                case string s:
                    if (!DateTimeOffset.TryParse(s, out _))
                    {
                        throw new AccessPolicyConditionValidationException(
                            $"Field '{fieldName}' expects a valid date/time value.");
                    }

                    return;
                default:
                    throw new AccessPolicyConditionValidationException(
                        $"Field '{fieldName}' expects a date/time value.");
            }
        }

        if (nonNullable == typeof(Guid))
        {
            switch (value)
            {
                case Guid:
                    return;
                case string s:
                    if (!Guid.TryParse(s, out _))
                    {
                        throw new AccessPolicyConditionValidationException(
                            $"Field '{fieldName}' expects a valid GUID value.");
                    }

                    return;
                default:
                    throw new AccessPolicyConditionValidationException(
                        $"Field '{fieldName}' expects a GUID value.");
            }
        }

        if (nonNullable.IsEnum)
        {
            switch (value)
            {
                case string s:
                    if (!Enum.TryParse(nonNullable, s, ignoreCase: true, out _))
                    {
                        throw new AccessPolicyConditionValidationException(
                            $"Field '{fieldName}' expects a valid {nonNullable.Name} value.");
                    }

                    return;
                case long l:
                    if (!Enum.IsDefined(nonNullable, checked((int)l)))
                    {
                        throw new AccessPolicyConditionValidationException(
                            $"Field '{fieldName}' expects a valid {nonNullable.Name} value.");
                    }

                    return;
                case int i:
                    if (!Enum.IsDefined(nonNullable, i))
                    {
                        throw new AccessPolicyConditionValidationException(
                            $"Field '{fieldName}' expects a valid {nonNullable.Name} value.");
                    }

                    return;
                default:
                    throw new AccessPolicyConditionValidationException(
                        $"Field '{fieldName}' expects an enum value.");
            }
        }

        if (IsNumericType(nonNullable))
        {
            if (value is not (long or double or int))
            {
                throw new AccessPolicyConditionValidationException(
                    $"Field '{fieldName}' expects a numeric value.");
            }

            return;
        }

        throw new AccessPolicyConditionValidationException(
            $"Field '{fieldName}' has unsupported CLR type '{targetType.Name}' for validation.");
    }

    private static bool IsNumericType(Type type) =>
        type == typeof(int) || type == typeof(long) || type == typeof(short)
        || type == typeof(byte) || type == typeof(float) || type == typeof(double) || type == typeof(decimal);
}

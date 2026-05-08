using algo.Application.Abstractions;
using algo.Application.Common.AccessPolicy;

namespace algo.Persistence.Abac;

internal static class AccessPolicyConditionTokenResolver
{
    public static AccessPolicyConditionAst Resolve(AccessPolicyConditionAst ast, IAccessPolicyTokenResolver tokens) =>
        ast switch
        {
            AccessPolicyAllAst all => new AccessPolicyAllAst(all.All.Select(a => Resolve(a, tokens)).ToList()),
            AccessPolicyAnyAst any => new AccessPolicyAnyAst(any.Any.Select(a => Resolve(a, tokens)).ToList()),
            AccessPolicyFieldAst field => new AccessPolicyFieldAst(
                field.Field,
                field.Operator,
                ResolveFieldValue(field, tokens)),
            _ => ast,
        };

    private static object? ResolveFieldValue(AccessPolicyFieldAst field, IAccessPolicyTokenResolver tokens)
    {
        if (field.Operator is "isNull" or "notNull")
        {
            return null;
        }

        var value = field.Value;

        if (field.Operator is "in" or "nin")
        {
            if (value is string roleToken
                && roleToken.Equals("@CurrentRoleNames", StringComparison.Ordinal))
            {
                return tokens.CurrentRoleNames.Select(r => (object?)r).ToList();
            }
        }

        if (value is string s && s.StartsWith('@'))
        {
            var resolved = tokens.ResolveTokenValue(s);
            if (resolved is null && s.Equals("@CurrentUserId", StringComparison.Ordinal))
            {
                return null;
            }

            if (resolved is IReadOnlyList<string> roles && field.Operator is "in" or "nin")
            {
                return roles.Select(r => (object?)r).ToList();
            }

            return resolved;
        }

        if (value is List<object?> list)
        {
            var mapped = new List<object?>(list.Count);
            foreach (var item in list)
            {
                if (item is string s2 && s2.StartsWith('@'))
                {
                    var resolvedItem = tokens.ResolveTokenValue(s2);
                    if (resolvedItem is IReadOnlyList<string> roleList && field.Operator is "in" or "nin")
                    {
                        foreach (var r in roleList)
                        {
                            mapped.Add(r);
                        }

                        continue;
                    }

                    mapped.Add(resolvedItem ?? s2);
                }
                else
                {
                    mapped.Add(item);
                }
            }

            return mapped;
        }

        return value;
    }
}

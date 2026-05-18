namespace algo.Application.Common.CustomFields;

public static class CustomFieldEntities
{
    public const string Users = "users";
    public const string Roles = "roles";
    public const string AccessPolicies = "accessPolicies";

    public static readonly IReadOnlySet<string> Supported =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Users,
            Roles,
            AccessPolicies
        };
}

namespace algo.Application.Common.CustomFields;

public static class CustomFieldEntities
{
    public const string Users = "users";
    public const string Roles = "roles";
    public const string AccessPolicies = "accessPolicies";
    public const string Products = "products";
    public const string Orders = "orders";

    public static readonly IReadOnlySet<string> Supported =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Users,
            Roles,
            AccessPolicies,
            Products,
            Orders
        };
}

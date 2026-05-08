namespace algo.Application.Features.Users.Validation;

public static class UserSortFields
{
    public const string Email = "email";

    public const string DisplayName = "displayName";

    public const string CreatedAt = "createdAt";

    public const string LastLoginAt = "lastLoginAt";

    public const string Status = "status";

    public static readonly HashSet<string> Allowed = new(StringComparer.OrdinalIgnoreCase)
    {
        Email,
        DisplayName,
        CreatedAt,
        LastLoginAt,
        Status,
    };
}

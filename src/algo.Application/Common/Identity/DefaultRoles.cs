namespace algo.Application.Common.Identity;

public static class DefaultRoles
{
    public const string Admin = "Admin";

    public const string User = "User";

    public static IReadOnlyList<string> All { get; } = [Admin, User];
}

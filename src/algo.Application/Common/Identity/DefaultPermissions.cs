namespace algo.Application.Common.Identity;

public static class DefaultPermissions
{
    public const string WildcardResource = "*";

    public const string WildcardAction = "*";

    public const int AdminFullAccessPriority = 1;

    public const string AdminFullAccessDescription = "Admin has full access to all resources and actions";
}

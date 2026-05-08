namespace algo.Application.Common.Identity;

public static class DefaultAdmin
{
    public const string Email = "admin@algo.bytes";

    public const string UserName = "admin";

    public const string DisplayName = "Super Admin";

    // TODO: Move this to secure configuration and rotate in production.
    public const string Password = "Admin@123456";
}

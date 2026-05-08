namespace algo.Application.Features.Roles.Validation;

internal static class RoleValidationConstants
{
    public const int MinNameLength = 2;

    public const int MaxNameLength = 256;

    public const string AllowedNameCharactersPattern = @"^[A-Za-z0-9 _-]+$";

    public const string AllowedNameCharactersMessage =
        "Role name may only contain letters, digits, spaces, underscores, and hyphens.";
}

using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;

namespace algo.Application.Features.Users;

internal static class IdentityResultExtensions
{
    public static void ThrowIfFailed(this IdentityResult result, string defaultProperty = "")
    {
        if (result.Succeeded)
            return;

        var failures = result.Errors.Select(e =>
            new ValidationFailure(MapProperty(e.Code, defaultProperty), e.Description));
        throw new ValidationException(failures);
    }

    private static string MapProperty(string? code, string fallback) => code switch
    {
        "DuplicateEmail" => "Email",
        "DuplicateUserName" => "UserName",
        "PasswordTooShort" or "PasswordRequiresDigit" or "PasswordRequiresLower"
            or "PasswordRequiresUpper" or "PasswordRequiresNonAlphanumeric" => "Password",
        _ => string.IsNullOrEmpty(fallback) ? string.Empty : fallback,
    };
}

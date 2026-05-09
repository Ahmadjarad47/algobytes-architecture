using algo.Application.Abstractions;
using algo.Application.Features.Auth.Dtos;
using algo.Application.Identity;
using algo.Domain.Identity.Entities;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Identity;
using System.Text.Encodings.Web;

namespace algo.Application.Features.Auth.Commands.Login;

public sealed class LoginCommandHandler(
    UserManager<ApplicationUser> userManager,
    IJwtTokenService jwt,
    IApplicationDbContext db,
    ISessionContext sessionContext) : IRequestHandler<LoginCommand, LoginResponseDto>
{
    public async Task<LoginResponseDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(string.Empty, "Invalid email or password."),
            });
        }

        if (!user.EmailConfirmed)
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(LoginCommand.Email), "Email is not verified. Complete OTP activation first."),
            });
        }

        var requiresTotp = user.TotpRequiredByAdmin || user.TwoFactorEnabled;
        if (requiresTotp)
        {
            if (string.IsNullOrWhiteSpace(request.TotpCode))
            {
                var setupRequired = !user.TwoFactorEnabled;
                var key = setupRequired
                    ? await EnsureAuthenticatorKeyAsync(userManager, user)
                    : null;

                return new LoginResponseDto(
                    null,
                    null,
                    new TotpChallengeDto(
                        true,
                        setupRequired,
                        key is null ? null : FormatKey(key),
                        key is null ? null : BuildOtpAuthUri(user.Email ?? user.UserName ?? user.Id, key),
                        setupRequired
                            ? "Two-factor authentication is required. Scan the QR code and enter a code to continue."
                            : "Two-factor authentication is required. Enter your authenticator app code."));
            }

            var cleanedCode = request.TotpCode.Replace(" ", string.Empty, StringComparison.Ordinal)
                .Replace("-", string.Empty, StringComparison.Ordinal);

            var isValidTotp = await userManager.VerifyTwoFactorTokenAsync(
                user,
                userManager.Options.Tokens.AuthenticatorTokenProvider,
                cleanedCode);

            if (!isValidTotp)
            {
                throw new ValidationException(new[]
                {
                    new ValidationFailure(nameof(LoginCommand.TotpCode), "Invalid authenticator code."),
                });
            }

            if (!user.TwoFactorEnabled)
            {
                await userManager.SetTwoFactorEnabledAsync(user, true);
            }
        }

        user.LastLoginAt = DateTimeOffset.UtcNow;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await userManager.UpdateAsync(user);

        var roles = (await userManager.GetRolesAsync(user)).ToArray();
        var auth = await AuthSessionIssuer.IssueAsync(user, roles, jwt, db, sessionContext, cancellationToken);
        return new LoginResponseDto(auth.User, auth.Tokens, null);
    }

    private static async Task<string> EnsureAuthenticatorKeyAsync(UserManager<ApplicationUser> userManager, ApplicationUser user)
    {
        var key = await userManager.GetAuthenticatorKeyAsync(user);
        if (!string.IsNullOrWhiteSpace(key))
        {
            return key;
        }

        await userManager.ResetAuthenticatorKeyAsync(user);
        key = await userManager.GetAuthenticatorKeyAsync(user);
        return key ?? string.Empty;
    }

    private static string BuildOtpAuthUri(string account, string key)
    {
        const string issuer = "algo.bytes";
        return $"otpauth://totp/{UrlEncoder.Default.Encode(issuer)}:{UrlEncoder.Default.Encode(account)}?secret={key}&issuer={UrlEncoder.Default.Encode(issuer)}&digits=6";
    }

    private static string FormatKey(string key)
    {
        var result = new List<string>();
        for (var i = 0; i < key.Length; i += 4)
        {
            result.Add(key.Substring(i, Math.Min(4, key.Length - i)));
        }

        return string.Join(' ', result).ToLowerInvariant();
    }
}

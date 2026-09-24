using algo.Application.Abstractions;
using algo.Application.Features.Auth.Dtos;
using algo.Application.Identity;
using algo.Domain.Identity.Entities;
using algo.Domain.Identity.Enums;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace algo.Application.Features.Auth.Commands.VerifyOtp;

public sealed class VerifyOtpCommandHandler(
    UserManager<ApplicationUser> userManager,
    IOtpCodeVerifier otpCodeVerifier,
    IAccessTokenFactory accessTokenFactory,
    IRefreshTokenFactory refreshTokenFactory,
    IApplicationDbContext db,
    ISessionContext sessionContext) : IRequestHandler<VerifyOtpCommand, AuthResponseDto>
{
    public async Task<AuthResponseDto> Handle(VerifyOtpCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(string.Empty, "Invalid email or verification code."),
            });
        }

        if (user.EmailConfirmed)
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(VerifyOtpCommand.Email), "This email is already verified."),
            });
        }

        var otp = await db.OtpTokens
            .Where(t => t.UserId == user.Id && t.Purpose == OtpPurpose.EmailConfirmation)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (otp is null
            || otp.ExpiresAt < DateTimeOffset.UtcNow
            || !otpCodeVerifier.VerifyCode(request.Code, otp.CodeHash))
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(VerifyOtpCommand.Code), "Invalid or expired verification code."),
            });
        }

        user.EmailConfirmed = true;
        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(string.Empty, "Could not activate account. Try again."),
            });
        }

        db.OtpTokens.Remove(otp);
        await db.SaveChangesAsync(cancellationToken);

        var roles = (await userManager.GetRolesAsync(user)).ToArray();
        return await AuthSessionIssuer.IssueAsync(
            user, roles, accessTokenFactory, refreshTokenFactory, db, sessionContext, cancellationToken);
    }
}

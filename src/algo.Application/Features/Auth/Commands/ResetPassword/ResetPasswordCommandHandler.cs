using algo.Application.Abstractions;
using algo.Domain.Identity.Entities;
using algo.Domain.Identity.Enums;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace algo.Application.Features.Auth.Commands.ResetPassword;

public sealed class ResetPasswordCommandHandler(
    UserManager<ApplicationUser> userManager,
    IOtpCodeVerifier otpCodeVerifier,
    IApplicationDbContext db) : IRequestHandler<ResetPasswordCommand, Unit>
{
    public async Task<Unit> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(string.Empty, "Invalid email or reset code."),
            });
        }

        var otp = await db.OtpTokens
            .Where(t => t.UserId == user.Id && t.Purpose == OtpPurpose.PasswordReset)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (otp is null
            || otp.ExpiresAt < DateTimeOffset.UtcNow
            || !otpCodeVerifier.VerifyCode(request.Code, otp.CodeHash))
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(ResetPasswordCommand.Code), "Invalid or expired reset code."),
            });
        }

        var identityToken = await userManager.GeneratePasswordResetTokenAsync(user);
        var resetResult = await userManager.ResetPasswordAsync(user, identityToken, request.NewPassword);
        if (!resetResult.Succeeded)
        {
            var failures = resetResult.Errors.Select(e => new ValidationFailure(
                nameof(ResetPasswordCommand.NewPassword),
                e.Description));
            throw new ValidationException(failures);
        }

        db.OtpTokens.Remove(otp);

        var refreshTokens = await db.RefreshTokens
            .Where(t => t.UserId == user.Id && t.RevokedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var rt in refreshTokens)
        {
            rt.RevokedAt = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}

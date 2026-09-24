using algo.Application.Abstractions;
using algo.Application.Configuration;
using algo.Application.Features.Auth.Dtos;
using algo.Domain.Identity.Entities;
using algo.Domain.Identity.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace algo.Application.Features.Auth.Commands.ForgotPassword;

public sealed class ForgotPasswordCommandHandler(
    UserManager<ApplicationUser> userManager,
    IApplicationDbContext db,
    IOtpCodeGenerator otpCodeGenerator,
    IPasswordResetEmailSender passwordResetEmailSender,
    IOptions<OtpOptions> otpOptions) : IRequestHandler<ForgotPasswordCommand, OtpVerificationDto>
{
    private readonly OtpOptions _otp = otpOptions.Value;

    public async Task<OtpVerificationDto> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null || !user.EmailConfirmed)
        {
            return new OtpVerificationDto(
                request.Email,
                DateTimeOffset.UtcNow.AddMinutes(_otp.ExpirationMinutes),
                "If an account exists for this email, a password reset code has been sent.");
        }

        var existing = await db.OtpTokens
            .Where(t => t.UserId == user.Id && t.Purpose == OtpPurpose.PasswordReset)
            .ToListAsync(cancellationToken);
        db.OtpTokens.RemoveRange(existing);

        var plainCode = otpCodeGenerator.GenerateNumericCode(_otp.CodeLength);
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(_otp.ExpirationMinutes);

        db.OtpTokens.Add(new OtpToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Purpose = OtpPurpose.PasswordReset,
            CodeHash = otpCodeGenerator.HashCode(plainCode),
            ExpiresAt = expiresAt,
            CreatedAt = DateTimeOffset.UtcNow,
        });

        await db.SaveChangesAsync(cancellationToken);

        await passwordResetEmailSender.SendPasswordResetOtpAsync(
            user.Email!,
            user.DisplayName,
            plainCode,
            expiresAt,
            cancellationToken);

        return new OtpVerificationDto(
            request.Email,
            expiresAt,
            "If an account exists for this email, a password reset code has been sent.");
    }
}

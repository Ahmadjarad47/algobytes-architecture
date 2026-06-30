using algo.Application.Abstractions;
using algo.Application.Configuration;
using algo.Application.Features.Auth.Dtos;
using algo.Domain.Identity.Entities;
using algo.Domain.Identity.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace algo.Application.Features.Auth.Commands.ResendOtp;

public sealed class ResendOtpCommandHandler(
    UserManager<ApplicationUser> userManager,
    IApplicationDbContext db,
    IOtpCodeGenerator otpCodeGenerator,
    IEmailConfirmationSender emailConfirmationSender,
    IOptions<OtpOptions> otpOptions) : IRequestHandler<ResendOtpCommand, OtpVerificationDto>
{
    private readonly OtpOptions _otp = otpOptions.Value;

    public async Task<OtpVerificationDto> Handle(ResendOtpCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null || user.EmailConfirmed)
        {
            return new OtpVerificationDto(
                request.Email,
                DateTimeOffset.UtcNow.AddMinutes(_otp.ExpirationMinutes),
                "If this account exists and is awaiting verification, a new code has been sent.");
        }

        var existing = await db.OtpTokens
            .Where(t => t.UserId == user.Id && t.Purpose == OtpPurpose.EmailConfirmation)
            .ToListAsync(cancellationToken);
        db.OtpTokens.RemoveRange(existing);

        var plainCode = otpCodeGenerator.GenerateNumericCode(_otp.CodeLength);
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(_otp.ExpirationMinutes);

        db.OtpTokens.Add(new OtpToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Purpose = OtpPurpose.EmailConfirmation,
            CodeHash = otpCodeGenerator.HashCode(plainCode),
            ExpiresAt = expiresAt,
            CreatedAt = DateTimeOffset.UtcNow,
        });

        await db.SaveChangesAsync(cancellationToken);

        await emailConfirmationSender.SendEmailConfirmationOtpAsync(
            user.Email!,
            user.DisplayName,
            plainCode,
            expiresAt,
            cancellationToken);

        return new OtpVerificationDto(
            user.Email!,
            expiresAt,
            "A new verification code has been sent.");
    }
}

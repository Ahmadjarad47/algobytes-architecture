using algo.Application.Abstractions;
using algo.Application.Configuration;
using algo.Application.Features.Auth.Dtos;
using algo.Domain.Identity.Entities;
using algo.Domain.Identity.Enums;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace algo.Application.Features.Auth.Commands.Register;

public sealed class RegisterCommandHandler(
    UserManager<ApplicationUser> userManager,
    IApplicationDbContext db,
    IOtpService otpService,
    IEmailService emailService,
    IOptions<OtpOptions> otpOptions) : IRequestHandler<RegisterCommand, OtpVerificationDto>
{
    private readonly OtpOptions _otp = otpOptions.Value;

    public async Task<OtpVerificationDto> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            DisplayName = request.DisplayName,
            EmailConfirmed = false,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            IsActive = true,
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var failures = result.Errors.Select(e => new ValidationFailure(
                MapErrorToProperty(e.Code),
                e.Description));
            throw new ValidationException(failures);
        }

        var plainCode = otpService.GenerateNumericCode(_otp.CodeLength);
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(_otp.ExpirationMinutes);

        db.OtpTokens.Add(new OtpToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Purpose = OtpPurpose.EmailConfirmation,
            CodeHash = otpService.HashCode(plainCode),
            ExpiresAt = expiresAt,
            CreatedAt = DateTimeOffset.UtcNow,
        });

        await db.SaveChangesAsync(cancellationToken);

        await emailService.SendEmailConfirmationOtpAsync(
            user.Email!,
            user.DisplayName,
            plainCode,
            expiresAt,
            cancellationToken);

        return new OtpVerificationDto(
            user.Email!,
            expiresAt,
            "Registration successful. Please verify your email with the code we sent.");
    }

    private static string MapErrorToProperty(string? code) => code switch
    {
        "DuplicateUserName" or "DuplicateEmail" => nameof(RegisterCommand.Email),
        "PasswordTooShort" or "PasswordRequiresDigit" or "PasswordRequiresLower" or "PasswordRequiresUpper"
            or "PasswordRequiresNonAlphanumeric" => nameof(RegisterCommand.Password),
        _ => string.Empty,
    };
}

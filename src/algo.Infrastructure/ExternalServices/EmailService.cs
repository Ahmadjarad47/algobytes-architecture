using algo.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace algo.Infrastructure.ExternalServices;

public sealed class EmailService(ILogger<EmailService> logger) : IEmailService
{
    public Task SendEmailConfirmationOtpAsync(
        string toEmail,
        string displayName,
        string code,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Email confirmation OTP for {Email} ({DisplayName}): {Code} (expires {ExpiresAt})",
            toEmail,
            displayName,
            code,
            expiresAt);
        return Task.CompletedTask;
    }

    public Task SendPasswordResetOtpAsync(
        string toEmail,
        string displayName,
        string code,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Password reset OTP for {Email} ({DisplayName}): {Code} (expires {ExpiresAt})",
            toEmail,
            displayName,
            code,
            expiresAt);
        return Task.CompletedTask;
    }
}

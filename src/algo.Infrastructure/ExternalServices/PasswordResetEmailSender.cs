using algo.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace algo.Infrastructure.ExternalServices;

public sealed class PasswordResetEmailSender(ILogger<PasswordResetEmailSender> logger)
    : BaseEmailService(logger), IPasswordResetEmailSender
{
    public Task SendPasswordResetOtpAsync(
        string toEmail,
        string displayName,
        string code,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken) =>
        LogOtpEmailAsync("Password reset OTP", toEmail, displayName, code, expiresAt);
}

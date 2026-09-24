using algo.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace algo.Infrastructure.ExternalServices;

public sealed class EmailConfirmationSender(ILogger<EmailConfirmationSender> logger)
    : BaseEmailService(logger), IEmailConfirmationSender
{
    public Task SendEmailConfirmationOtpAsync(
        string toEmail,
        string displayName,
        string code,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken) =>
        LogOtpEmailAsync("Email confirmation OTP", toEmail, displayName, code, expiresAt);
}

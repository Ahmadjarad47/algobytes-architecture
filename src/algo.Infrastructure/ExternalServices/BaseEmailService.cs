using Microsoft.Extensions.Logging;

namespace algo.Infrastructure.ExternalServices;

public abstract class BaseEmailService(ILogger logger)
{
    protected Task LogOtpEmailAsync(
        string subject,
        string toEmail,
        string displayName,
        string code,
        DateTimeOffset expiresAt)
    {
        logger.LogInformation(
            "{Subject} for {Email} ({DisplayName}): {Code} (expires {ExpiresAt})",
            subject,
            toEmail,
            displayName,
            code,
            expiresAt);
        return Task.CompletedTask;
    }
}

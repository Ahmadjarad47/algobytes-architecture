namespace algo.Application.Abstractions.Identity;

public interface IEmailConfirmationSender
{
    Task SendEmailConfirmationOtpAsync(
        string toEmail,
        string displayName,
        string code,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken);
}


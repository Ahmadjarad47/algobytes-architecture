namespace algo.Application.Abstractions;

public interface IEmailConfirmationSender
{
    Task SendEmailConfirmationOtpAsync(
        string toEmail,
        string displayName,
        string code,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken);
}

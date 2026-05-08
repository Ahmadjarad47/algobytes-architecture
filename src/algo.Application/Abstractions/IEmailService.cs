namespace algo.Application.Abstractions;

public interface IEmailService
{
    Task SendEmailConfirmationOtpAsync(
        string toEmail,
        string displayName,
        string code,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken);

    Task SendPasswordResetOtpAsync(
        string toEmail,
        string displayName,
        string code,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken);
}

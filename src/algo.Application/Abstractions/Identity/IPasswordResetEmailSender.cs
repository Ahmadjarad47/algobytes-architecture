namespace algo.Application.Abstractions.Identity;

public interface IPasswordResetEmailSender
{
    Task SendPasswordResetOtpAsync(
        string toEmail,
        string displayName,
        string code,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken);
}


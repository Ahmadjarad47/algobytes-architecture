namespace algo.Domain.Identity.Entities;

public sealed class RefreshToken
{
    public Guid Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = null!;

    public string TokenHash { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset LastActivityAt { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }

    public string? RevokedByUserId { get; set; }

    public string? ReplacedByTokenHash { get; set; }

    public string? IpAddress { get; set; }

    public string? Location { get; set; }

    public string? Device { get; set; }

    public string? Browser { get; set; }

    public string? OperatingSystem { get; set; }

    public string? UserAgent { get; set; }

    public bool IsTrustedDevice { get; set; }

    public bool IsSuspicious { get; set; }
}

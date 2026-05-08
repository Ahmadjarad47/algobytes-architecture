using algo.Domain.Identity.Enums;

namespace algo.Domain.Identity.Entities;

public sealed class OtpToken
{
    public Guid Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = null!;

    public OtpPurpose Purpose { get; set; }

    public string CodeHash { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}

using Microsoft.AspNetCore.Identity;

namespace algo.Domain.Identity.Entities;

public sealed class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public DateTimeOffset? LastLoginAt { get; set; }

    public string? CreatedByUserId { get; set; }
    
    public bool TotpRequiredByAdmin { get; set; }

    public ICollection<OtpToken> OtpTokens { get; set; } = new List<OtpToken>();

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}

using System.Text.Json;
using algo.Domain.CustomFields;
using algo.Domain.Sales.Entities;
using Microsoft.AspNetCore.Identity;

namespace algo.Domain.Identity.Entities;

public sealed class ApplicationUser : IdentityUser, IHasCustomFields
{
    public string DisplayName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTimeOffset? TrashedAt { get; set; }

    public DateTimeOffset? TrashExpiresAt { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public DateTimeOffset? LastLoginAt { get; set; }

    public string? CreatedByUserId { get; set; }
    
    public bool TotpRequiredByAdmin { get; set; }

    public JsonDocument? CustomFields { get; set; }

    public ICollection<OtpToken> OtpTokens { get; set; } = new List<OtpToken>();

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    public ICollection<Order> Orders { get; set; } = new List<Order>();

    public ICollection<WalletTransaction> WalletTransactions { get; set; } = new List<WalletTransaction>();
}

using algo.Domain.Identity.Entities;

namespace algo.Domain.Sales.Entities;

public class WalletTransaction
{
    public long Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string CurrencyCode { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string TransactionType { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? ReferenceId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public virtual ApplicationUser User { get; set; } = null!;
}

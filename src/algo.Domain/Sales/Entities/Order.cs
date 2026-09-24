using System.Text.Json;
using algo.Domain.CustomFields;
using algo.Domain.Identity.Entities;

namespace algo.Domain.Sales.Entities;

public class Order : IHasCustomFields
{
    public long Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string OrderNumber { get; set; } = string.Empty;

    public string CurrencyCode { get; set; } = string.Empty;

    public decimal TotalAmount { get; set; }

    public decimal? ExchangeRateUsedToBase { get; set; }

    public string? PaymentMethod { get; set; }

    public string OrderStatus { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public JsonDocument? CustomFields { get; set; }

    public virtual ApplicationUser User { get; set; } = null!;

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}

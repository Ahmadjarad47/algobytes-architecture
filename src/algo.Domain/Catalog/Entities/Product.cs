using System.Text.Json;
using algo.Domain.CustomFields;

namespace algo.Domain.Catalog.Entities;

public class Product : IHasCustomFields
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int CategoryId { get; set; }

    public string CurrencyCode { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public decimal? DiscountedPrice { get; set; }

    public JsonDocument? CustomFields { get; set; }

    public decimal? PriceUsd { get; set; }

    public decimal? PriceSyp { get; set; }

    public decimal? DiscountedPriceUsd { get; set; }

    public decimal? DiscountedPriceSyp { get; set; }

    public string? ExternalGameId { get; set; }

    public string? Provider { get; set; }

    public string? ImageUrl { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public DateTimeOffset? TrashedAt { get; set; }

    public DateTimeOffset? TrashExpiresAt { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public virtual Category Category { get; set; } = null!;
}

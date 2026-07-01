namespace algo.Domain.Catalog.Entities;

public class Product
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int CategoryId { get; set; }

    public decimal? PriceUsd { get; set; }

    public decimal? PriceSyp { get; set; }

    public decimal? DiscountedPriceUsd { get; set; }

    public decimal? DiscountedPriceSyp { get; set; }

    public string? ExternalGameId { get; set; }

    public string? Provider { get; set; }

    public string? ImageUrl { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public virtual Category Category { get; set; } = null!;
}

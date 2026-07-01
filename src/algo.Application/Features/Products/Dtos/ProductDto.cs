namespace algo.Application.Features.Products.Dtos;

public sealed record ProductDto(
    int Id,
    string Name,
    int CategoryId,
    string CategoryName,
    decimal? PriceUsd,
    decimal? PriceSyp,
    decimal? DiscountedPriceUsd,
    decimal? DiscountedPriceSyp,
    string? ExternalGameId,
    string? Provider,
    string? ImageUrl,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

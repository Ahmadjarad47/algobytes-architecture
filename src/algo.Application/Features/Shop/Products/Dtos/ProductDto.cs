using System.Text.Json;

namespace algo.Application.Features.Shop.Products.Dtos;

public sealed record ProductDto(
    int Id,
    string Name,
    int CategoryId,
    string CategoryName,
    string CurrencyCode,
    decimal Price,
    decimal? DiscountedPrice,
    JsonElement? CustomFields,
    string? ImageUrl,
    DateTimeOffset CreatedAt);

using System.Text.Json;
using algo.Application.Features.Shop.Products.Dtos;
using MediatR;

namespace algo.Application.Features.Shop.Products.Commands.UpdateProduct;

public sealed record UpdateProductCommand(
    int Id,
    string Name,
    int CategoryId,
    string CurrencyCode,
    decimal Price,
    decimal? DiscountedPrice,
    JsonDocument? CustomFields,
    string? ImageUrl) : IRequest<ProductDto?>;

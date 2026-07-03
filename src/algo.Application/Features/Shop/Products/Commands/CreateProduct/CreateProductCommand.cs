using System.Text.Json;
using algo.Application.Features.Shop.Products.Dtos;
using MediatR;

namespace algo.Application.Features.Shop.Products.Commands.CreateProduct;

public sealed record CreateProductCommand(
    string Name,
    int CategoryId,
    string CurrencyCode,
    decimal Price,
    decimal? DiscountedPrice,
    JsonDocument? CustomFields,
    string? ImageUrl) : IRequest<ProductDto>;

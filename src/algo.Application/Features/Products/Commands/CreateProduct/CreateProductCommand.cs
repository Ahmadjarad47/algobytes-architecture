using algo.Application.Features.Products.Dtos;
using MediatR;

namespace algo.Application.Features.Products.Commands.CreateProduct;

public sealed record CreateProductCommand(
    string Name,
    int CategoryId,
    decimal? PriceUsd,
    decimal? PriceSyp,
    decimal? DiscountedPriceUsd,
    decimal? DiscountedPriceSyp,
    string? ExternalGameId,
    string? Provider,
    string? ImageUrl) : IRequest<ProductDto>;

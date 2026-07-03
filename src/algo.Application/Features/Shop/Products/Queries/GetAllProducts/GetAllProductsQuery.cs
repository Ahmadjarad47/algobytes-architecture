using algo.Application.Features.Shop.Products.Dtos;
using MediatR;

namespace algo.Application.Features.Shop.Products.Queries.GetAllProducts;

public sealed record GetAllProductsQuery : IRequest<IReadOnlyList<ProductDto>>;

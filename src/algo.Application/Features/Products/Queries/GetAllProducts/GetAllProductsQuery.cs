using algo.Application.Features.Products.Dtos;
using MediatR;

namespace algo.Application.Features.Products.Queries.GetAllProducts;

public sealed record GetAllProductsQuery : IRequest<IReadOnlyList<ProductDto>>;

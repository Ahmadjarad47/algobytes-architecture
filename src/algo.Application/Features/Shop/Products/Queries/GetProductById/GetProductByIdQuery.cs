using algo.Application.Features.Shop.Products.Dtos;
using MediatR;

namespace algo.Application.Features.Shop.Products.Queries.GetProductById;

public sealed record GetProductByIdQuery(int Id) : IRequest<ProductDto?>;

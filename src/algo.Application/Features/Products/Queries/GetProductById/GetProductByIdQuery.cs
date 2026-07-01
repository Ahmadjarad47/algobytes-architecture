using algo.Application.Features.Products.Dtos;
using MediatR;

namespace algo.Application.Features.Products.Queries.GetProductById;

public sealed record GetProductByIdQuery(int Id) : IRequest<ProductDto?>;

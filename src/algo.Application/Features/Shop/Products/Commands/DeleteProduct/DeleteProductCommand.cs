using MediatR;

namespace algo.Application.Features.Shop.Products.Commands.DeleteProduct;

public sealed record DeleteProductCommand(int Id) : IRequest<bool>;

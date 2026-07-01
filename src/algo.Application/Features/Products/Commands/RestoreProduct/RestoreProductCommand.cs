using MediatR;

namespace algo.Application.Features.Products.Commands.RestoreProduct;

public sealed record RestoreProductCommand(int Id) : IRequest<bool>;

using algo.Application.Abstractions;
using algo.Application.Abstractions.Persistence;
using algo.Application.Common.AccessPolicy;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace algo.Application.Features.Products.Commands.DeleteProduct;

public sealed class DeleteProductCommandHandler(
    IApplicationDbContext db,
    IAccessPolicyAuthorizationChecker authorizationChecker)
    : IRequestHandler<DeleteProductCommand, bool>
{
    public async Task<bool> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        await authorizationChecker.EnsureResourceActionAllowedAsync(
            db,
            AccessPolicyResources.Products,
            AccessPolicyActions.Delete,
            cancellationToken);

        var product = await db.Products
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (product is null)
            return false;

        db.Products.Remove(product);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}

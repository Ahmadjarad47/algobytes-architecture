using algo.Application.Abstractions;
using algo.Application.Abstractions.Persistence;
using algo.Application.Common.AccessPolicy;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace algo.Application.Features.Products.Commands.RestoreProduct;

public sealed class RestoreProductCommandHandler(
    IApplicationDbContext db,
    IAccessPolicyAuthorizationChecker authorizationChecker)
    : IRequestHandler<RestoreProductCommand, bool>
{
    public async Task<bool> Handle(RestoreProductCommand request, CancellationToken cancellationToken)
    {
        await authorizationChecker.EnsureResourceActionAllowedAsync(
            db,
            AccessPolicyResources.Products,
            AccessPolicyActions.Update,
            cancellationToken);

        var product = await db.Products
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                p => p.Id == request.Id && p.TrashedAt != null && p.DeletedAt == null,
                cancellationToken);

        if (product is null)
        {
            return false;
        }

        product.TrashedAt = null;
        product.TrashExpiresAt = null;
        product.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}

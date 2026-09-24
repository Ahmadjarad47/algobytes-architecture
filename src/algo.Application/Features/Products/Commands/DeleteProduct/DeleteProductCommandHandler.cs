using algo.Application.Abstractions;
using algo.Application.Abstractions.Persistence;
using algo.Application.Common.AccessPolicy;
using algo.Application.Common.Trash;
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
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                p => p.Id == request.Id && p.DeletedAt == null,
                cancellationToken);

        if (product is null)
            return false;

        var utcNow = DateTimeOffset.UtcNow;
        product.TrashedAt = utcNow;
        product.TrashExpiresAt = utcNow.Add(TrashRetention.Duration);
        product.UpdatedAt = utcNow;

        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}

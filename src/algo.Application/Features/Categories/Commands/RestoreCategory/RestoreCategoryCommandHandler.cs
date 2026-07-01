using algo.Application.Abstractions;
using algo.Application.Abstractions.Persistence;
using algo.Application.Common.AccessPolicy;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace algo.Application.Features.Categories.Commands.RestoreCategory;

public sealed class RestoreCategoryCommandHandler(
    IApplicationDbContext db,
    IAccessPolicyAuthorizationChecker authorizationChecker)
    : IRequestHandler<RestoreCategoryCommand, bool>
{
    public async Task<bool> Handle(RestoreCategoryCommand request, CancellationToken cancellationToken)
    {
        await authorizationChecker.EnsureResourceActionAllowedAsync(
            db,
            AccessPolicyResources.Categories,
            AccessPolicyActions.Update,
            cancellationToken);

        var category = await db.Categories
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                c => c.Id == request.Id && c.TrashedAt != null && c.DeletedAt == null,
                cancellationToken);

        if (category is null)
        {
            return false;
        }

        category.TrashedAt = null;
        category.TrashExpiresAt = null;

        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}

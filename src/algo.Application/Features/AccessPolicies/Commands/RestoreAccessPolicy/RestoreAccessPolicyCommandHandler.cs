using algo.Application.Abstractions;
using algo.Application.Common.AccessPolicy;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace algo.Application.Features.AccessPolicies.Commands.RestoreAccessPolicy;

public sealed class RestoreAccessPolicyCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IAccessPolicyEvaluator accessPolicyEvaluator)
    : IRequestHandler<RestoreAccessPolicyCommand, bool>
{
    public async Task<bool> Handle(RestoreAccessPolicyCommand request, CancellationToken cancellationToken)
    {
        await accessPolicyEvaluator.EnsureResourceActionAllowedAsync(
            db,
            AccessPolicyResources.AccessPolicies,
            AccessPolicyActions.Update,
            cancellationToken);

        var entity = await db.AccessPolicies
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == request.Id && p.TrashedAt != null && p.DeletedAt == null, cancellationToken);

        if (entity is null)
        {
            return false;
        }

        entity.TrashedAt = null;
        entity.TrashExpiresAt = null;
        entity.UpdatedByUserId = currentUser.UserId;

        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}

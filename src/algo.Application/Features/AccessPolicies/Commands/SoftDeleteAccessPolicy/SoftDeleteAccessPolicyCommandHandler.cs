using algo.Application.Abstractions;
using algo.Application.Common.AccessPolicy;
using algo.Application.Common.Trash;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace algo.Application.Features.AccessPolicies.Commands.SoftDeleteAccessPolicy;

public sealed class SoftDeleteAccessPolicyCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IAccessPolicyEvaluator accessPolicyEvaluator)
    : IRequestHandler<SoftDeleteAccessPolicyCommand, bool>
{
    public async Task<bool> Handle(SoftDeleteAccessPolicyCommand request, CancellationToken cancellationToken)
    {
        await accessPolicyEvaluator.EnsureResourceActionAllowedAsync(
            db,
            AccessPolicyResources.AccessPolicies,
            AccessPolicyActions.Delete,
            cancellationToken);

        var entity = await db.AccessPolicies
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == request.Id && p.DeletedAt == null, cancellationToken);

        if (entity is null)
        {
            return false;
        }

        entity.TrashedAt = DateTime.UtcNow;
        entity.TrashExpiresAt = entity.TrashedAt.Value.Add(TrashRetention.Duration);
        entity.UpdatedByUserId = currentUser.UserId;
        entity.IsEnabled = false;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}

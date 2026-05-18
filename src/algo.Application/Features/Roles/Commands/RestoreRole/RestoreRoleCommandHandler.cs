using algo.Application.Abstractions;
using algo.Application.Common.AccessPolicy;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace algo.Application.Features.Roles.Commands.RestoreRole;

public sealed class RestoreRoleCommandHandler(
    IAccessPolicyEvaluator accessPolicyEvaluator,
    IApplicationDbContext db)
    : IRequestHandler<RestoreRoleCommand, bool>
{
    public async Task<bool> Handle(RestoreRoleCommand request, CancellationToken cancellationToken)
    {
        await accessPolicyEvaluator.EnsureResourceActionAllowedAsync(
            db,
            AccessPolicyResources.Roles,
            AccessPolicyActions.Update,
            cancellationToken);

        var role = await db.Roles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.Id == request.Id && r.TrashedAt != null && r.DeletedAt == null, cancellationToken);

        if (role is null)
        {
            return false;
        }

        role.TrashedAt = null;
        role.TrashExpiresAt = null;

        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}

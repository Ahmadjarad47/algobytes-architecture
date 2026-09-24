using algo.Application.Abstractions;
using algo.Application.Common.AccessPolicy;
using algo.Application.Common.Trash;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace algo.Application.Features.Roles.Commands.DeleteRole;

public sealed class DeleteRoleCommandHandler(
    IAccessPolicyAuthorizationChecker authorizationChecker,
    IApplicationDbContext db)
    : IRequestHandler<DeleteRoleCommand, bool>
{
    public async Task<bool> Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
    {
        await authorizationChecker.EnsureResourceActionAllowedAsync(
            db,
            AccessPolicyResources.Roles,
            AccessPolicyActions.Delete,
            cancellationToken);

        var role = await db.Roles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.Id == request.Id && r.DeletedAt == null, cancellationToken);
        if (role is null)
            return false;

        var utcNow = DateTimeOffset.UtcNow;
        role.TrashedAt = utcNow;
        role.TrashExpiresAt = utcNow.Add(TrashRetention.Duration);

        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}

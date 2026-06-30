using algo.Application.Abstractions;
using algo.Application.Common.AccessPolicy;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace algo.Application.Features.Users.Commands.RestoreUser;

public sealed class RestoreUserCommandHandler(
    IAccessPolicyAuthorizationChecker authorizationChecker,
    IApplicationDbContext db)
    : IRequestHandler<RestoreUserCommand, bool>
{
    public async Task<bool> Handle(RestoreUserCommand request, CancellationToken cancellationToken)
    {
        await authorizationChecker.EnsureResourceActionAllowedAsync(
            db,
            AccessPolicyResources.Users,
            AccessPolicyActions.Update,
            cancellationToken);

        var user = await db.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == request.UserId && u.TrashedAt != null && u.DeletedAt == null, cancellationToken);

        if (user is null)
        {
            return false;
        }

        user.TrashedAt = null;
        user.TrashExpiresAt = null;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}

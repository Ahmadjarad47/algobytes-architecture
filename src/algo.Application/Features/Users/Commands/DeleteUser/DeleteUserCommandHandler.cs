using algo.Application.Abstractions;
using algo.Application.Common.AccessPolicy;
using algo.Application.Common.Trash;
using algo.Application.Features.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace algo.Application.Features.Users.Commands.DeleteUser;

public sealed class DeleteUserCommandHandler(
    IAccessPolicyEvaluator accessPolicyEvaluator,
    IApplicationDbContext db)
    : IRequestHandler<DeleteUserCommand, bool>
{
    public async Task<bool> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        await accessPolicyEvaluator.EnsureResourceActionAllowedAsync(
            db,
            AccessPolicyResources.Users,
            AccessPolicyActions.Delete,
            cancellationToken);

        var scoped = await accessPolicyEvaluator.ApplyAsync(
            db.Users.IgnoreQueryFilters().Where(u => u.Id == request.UserId && u.DeletedAt == null),
            AccessPolicyResources.Users,
            AccessPolicyActions.Delete,
            cancellationToken);

        if (!await scoped.AnyAsync(cancellationToken))
            return false;

        var user = await db.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == request.UserId && u.DeletedAt == null, cancellationToken);
        if (user is null)
            return false;

        var utcNow = DateTimeOffset.UtcNow;
        user.TrashedAt = utcNow;
        user.TrashExpiresAt = utcNow.Add(TrashRetention.Duration);
        user.UpdatedAt = utcNow;
        user.IsActive = false;

        var tokens = await db.RefreshTokens
            .Where(token => token.UserId == user.Id && token.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in tokens)
        {
            token.RevokedAt = utcNow;
        }

        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}

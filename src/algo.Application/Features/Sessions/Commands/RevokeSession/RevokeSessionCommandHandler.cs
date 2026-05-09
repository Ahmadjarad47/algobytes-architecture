using algo.Application.Abstractions;
using algo.Application.Common.AccessPolicy;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace algo.Application.Features.Sessions.Commands.RevokeSession;

public sealed class RevokeSessionCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IAccessPolicyEvaluator accessPolicyEvaluator,
    ISessionRealtimeNotifier sessionRealtimeNotifier) : IRequestHandler<RevokeSessionCommand, bool>
{
    public async Task<bool> Handle(RevokeSessionCommand request, CancellationToken cancellationToken)
    {
        await accessPolicyEvaluator.EnsureResourceActionAllowedAsync(
            db,
            AccessPolicyResources.Sessions,
            AccessPolicyActions.Revoke,
            cancellationToken);

        var session = await db.RefreshTokens.FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);
        if (session is null)
        {
            return false;
        }

        if (session.UserId == currentUser.UserId && !request.ConfirmCurrentSession)
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(
                    nameof(RevokeSessionCommand.ConfirmCurrentSession),
                    "You cannot revoke your current admin session without confirmation."),
            });
        }

        session.RevokedAt ??= DateTimeOffset.UtcNow;
        session.RevokedByUserId = currentUser.UserId;
        await db.SaveChangesAsync(cancellationToken);
        await sessionRealtimeNotifier.NotifySessionsRevokedAsync(
            new[] { session.Id },
            new[] { session.UserId },
            cancellationToken);

        return true;
    }
}

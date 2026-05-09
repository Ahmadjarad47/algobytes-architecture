using algo.Application.Abstractions;
using algo.Application.Common.AccessPolicy;
using algo.Application.Features.Sessions.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace algo.Application.Features.Sessions.Queries.GetSessionById;

public sealed class GetSessionByIdQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IAccessPolicyEvaluator accessPolicyEvaluator) : IRequestHandler<GetSessionByIdQuery, ActiveSessionDto?>
{
    public async Task<ActiveSessionDto?> Handle(GetSessionByIdQuery request, CancellationToken cancellationToken)
    {
        await accessPolicyEvaluator.EnsureResourceActionAllowedAsync(
            db,
            AccessPolicyResources.Sessions,
            AccessPolicyActions.Read,
            cancellationToken);

        var token = await db.RefreshTokens
            .AsNoTracking()
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

        if (token is null)
        {
            return null;
        }

        var role = await (
                from userRole in db.UserRoles.AsNoTracking()
                join identityRole in db.Roles.AsNoTracking() on userRole.RoleId equals identityRole.Id
                where userRole.UserId == token.UserId
                orderby identityRole.Name
                select identityRole.Name)
            .FirstOrDefaultAsync(cancellationToken) ?? "User";

        var now = DateTimeOffset.UtcNow;
        var status = token.RevokedAt != null
            ? "Revoked"
            : token.ExpiresAt <= now
                ? "Expired"
                : token.LastActivityAt < now.AddMinutes(-15)
                    ? "Idle"
                    : "Online";

        var row = new SessionRow(
            token.Id,
            token.UserId,
            token.User.DisplayName,
            token.User.Email ?? string.Empty,
            role,
            status,
            token.Device ?? "Unknown",
            token.Browser ?? "Unknown",
            token.OperatingSystem ?? "Unknown",
            token.IpAddress,
            token.Location,
            token.CreatedAt,
            token.LastActivityAt,
            token.ExpiresAt,
            token.ExpiresAt,
            token.UserId == currentUser.UserId && token.RevokedAt == null && token.ExpiresAt > now,
            token.IsTrustedDevice,
            token.IsSuspicious,
            token.RevokedAt,
            token.RevokedByUserId,
            token.UserAgent,
            0);

        return SessionMapping.ToDto(row);
    }
}

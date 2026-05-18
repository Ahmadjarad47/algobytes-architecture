using algo.Domain.Identity.Entities;
using Microsoft.AspNetCore.Identity;

namespace algo.Application.Features.Sessions;

internal static class SessionProjection
{
    public static IQueryable<SessionRow> Project(
        IQueryable<RefreshToken> tokens,
        IQueryable<IdentityUserRole<string>> userRoles,
        IQueryable<ApplicationRole> roles,
        string? currentUserId)
    {
        return
            from token in tokens
            from roleName in
                (from userRole in userRoles
                 join role in roles on userRole.RoleId equals role.Id
                 where userRole.UserId == token.UserId
                 orderby role.Name
                 select role.Name)
                .Take(1)
                .DefaultIfEmpty()
            select new SessionRow(
                token.Id,
                token.UserId,
                token.User.DisplayName,
                token.User.Email ?? string.Empty,
                roleName ?? "User",
                token.RevokedAt != null
                    ? "Revoked"
                    : token.ExpiresAt <= DateTimeOffset.UtcNow
                        ? "Expired"
                        : token.LastActivityAt < DateTimeOffset.UtcNow.AddMinutes(-15)
                            ? "Idle"
                            : "Online",
                token.Device ?? "Unknown",
                token.Browser ?? "Unknown",
                token.OperatingSystem ?? "Unknown",
                token.IpAddress,
                token.Location,
                token.CreatedAt,
                token.LastActivityAt,
                token.ExpiresAt,
                token.ExpiresAt,
                token.UserId == currentUserId && token.RevokedAt == null && token.ExpiresAt > DateTimeOffset.UtcNow,
                token.IsTrustedDevice,
                token.IsSuspicious,
                token.RevokedAt,
                token.RevokedByUserId,
                token.UserAgent,
                0);
    }
}

internal sealed record SessionRow(
    Guid Id,
    string UserId,
    string UserName,
    string Email,
    string Role,
    string Status,
    string Device,
    string Browser,
    string Os,
    string? IpAddress,
    string? Location,
    DateTimeOffset LoginTime,
    DateTimeOffset LastActivity,
    DateTimeOffset ExpiresAt,
    DateTimeOffset RefreshTokenExpiresAt,
    bool CurrentAdminSession,
    bool TrustedDevice,
    bool Suspicious,
    DateTimeOffset? RevokedAt,
    string? RevokedBy,
    string? UserAgent,
    int DurationMinutes);

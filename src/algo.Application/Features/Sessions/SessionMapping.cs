using algo.Application.Features.Sessions.Dtos;

namespace algo.Application.Features.Sessions;

internal static class SessionMapping
{
    public static ActiveSessionDto ToDto(SessionRow row)
    {
        var durationMinutes = Math.Max(
            1,
            (int)Math.Round((DateTimeOffset.UtcNow - row.LoginTime).TotalMinutes));

        return new ActiveSessionDto(
            row.Id,
            row.UserId,
            row.UserName,
            row.Email,
            row.Role,
            row.Status,
            row.Device,
            row.Browser,
            row.Os,
            row.IpAddress,
            row.Location,
            row.LoginTime,
            row.LastActivity,
            durationMinutes,
            row.ExpiresAt,
            row.RefreshTokenExpiresAt,
            row.CurrentAdminSession,
            row.TrustedDevice,
            row.Suspicious,
            row.RevokedAt,
            row.RevokedBy,
            row.UserAgent,
            BuildTimeline(row));
    }

    private static IReadOnlyList<string> BuildTimeline(SessionRow row) =>
        new[]
        {
            $"Login accepted from {row.IpAddress ?? "unknown IP"}",
            $"Last activity recorded at {row.LastActivity:u}",
            row.RevokedAt is null ? "Session remains tracked" : $"Session revoked by {row.RevokedBy ?? "unknown admin"}"
        };
}

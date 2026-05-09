using algo.Application.Abstractions;
using algo.Application.Common.AccessPolicy;
using algo.Application.Common.Pagination;
using algo.Application.Features.Sessions.Dtos;
using algo.Domain.Identity.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace algo.Application.Features.Sessions.Queries.GetSessions;

public sealed class GetSessionsQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IAccessPolicyEvaluator accessPolicyEvaluator)
    : IRequestHandler<GetSessionsQuery, ActiveSessionsResponseDto>
{
    public async Task<ActiveSessionsResponseDto> Handle(
        GetSessionsQuery request,
        CancellationToken cancellationToken)
    {
        await accessPolicyEvaluator.EnsureResourceActionAllowedAsync(
            db,
            AccessPolicyResources.Sessions,
            AccessPolicyActions.Read,
            cancellationToken);

        var tokenQuery = db.RefreshTokens
            .AsNoTracking()
            .Include(t => t.User)
            .Where(t => t.RevokedAt == null && t.ExpiresAt > DateTimeOffset.UtcNow)
            .AsQueryable();

        tokenQuery = ApplyDatabaseFilters(tokenQuery, request);

        var tokens = await tokenQuery
            .OrderByDescending(token => token.LastActivityAt)
            .ToListAsync(cancellationToken);

        var userIds = tokens.Select(token => token.UserId).Distinct().ToList();
        var rolesByUserId = await (
                from userRole in db.UserRoles.AsNoTracking()
                join role in db.Roles.AsNoTracking() on userRole.RoleId equals role.Id
                where userIds.Contains(userRole.UserId)
                orderby role.Name
                select new { userRole.UserId, role.Name })
            .GroupBy(row => row.UserId)
            .ToDictionaryAsync(
                group => group.Key,
                group => group.Select(row => row.Name).FirstOrDefault() ?? "User",
                cancellationToken);

        var rows = tokens
            .GroupBy(token => token.UserId)
            .Select(group => group
                .OrderByDescending(token => token.LastActivityAt)
                .ThenByDescending(token => token.CreatedAt)
                .First())
            .Select(token => ToSessionRow(token, rolesByUserId, currentUser.UserId))
            .Where(row => MatchesComputedFilters(row, request))
            .OrderByDescending(row => row.LastActivity)
            .ToList();

        var total = rows.Count;
        var pageNumber = Math.Max(1, request.Pagination.PageNumber);
        var pageSize = Math.Clamp(request.Pagination.PageSize, 1, 100);

        var pageRows = rows
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var items = pageRows.Select(SessionMapping.ToDto).ToList();
        var summary = BuildSummary(rows);

        return new ActiveSessionsResponseDto(
            new PaginatedResult<ActiveSessionDto>(items, pageNumber, pageSize, total),
            summary);
    }

    private static IQueryable<RefreshToken> ApplyDatabaseFilters(
        IQueryable<RefreshToken> query,
        GetSessionsQuery request)
    {
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(token =>
                token.User.DisplayName.ToLower().Contains(search) ||
                (token.User.Email != null && token.User.Email.ToLower().Contains(search)) ||
                (token.IpAddress != null && token.IpAddress.ToLower().Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(request.Device) && request.Device != "All")
        {
            query = query.Where(token => token.Device == request.Device);
        }

        if (!string.IsNullOrWhiteSpace(request.Browser) && request.Browser != "All")
        {
            query = query.Where(token => token.Browser == request.Browser);
        }

        if (request.From is { } from)
        {
            query = query.Where(token => token.CreatedAt >= from);
        }

        if (request.To is { } to)
        {
            query = query.Where(token => token.CreatedAt <= to);
        }

        if (request.SuspiciousOnly)
        {
            query = query.Where(token => token.IsSuspicious);
        }

        return query;
    }

    private static bool MatchesComputedFilters(SessionRow row, GetSessionsQuery request)
    {
        if (!string.IsNullOrWhiteSpace(request.Status) && request.Status != "All" && row.Status != request.Status)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(request.Role) && request.Role != "All" && row.Role != request.Role)
        {
            return false;
        }

        return true;
    }

    private static SessionRow ToSessionRow(
        RefreshToken token,
        IReadOnlyDictionary<string, string> rolesByUserId,
        string? currentUserId)
    {
        var now = DateTimeOffset.UtcNow;
        var role = rolesByUserId.TryGetValue(token.UserId, out var foundRole) ? foundRole : "User";
        var status = token.RevokedAt != null
            ? "Revoked"
            : token.ExpiresAt <= now
                ? "Expired"
                : token.LastActivityAt < now.AddMinutes(-15)
                    ? "Idle"
                    : "Online";

        return new SessionRow(
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
            token.UserId == currentUserId && token.RevokedAt == null && token.ExpiresAt > now,
            token.IsTrustedDevice,
            token.IsSuspicious,
            token.RevokedAt,
            token.RevokedByUserId,
            token.UserAgent,
            0);
    }

    private static ActiveSessionsSummaryDto BuildSummary(IReadOnlyCollection<SessionRow> rows)
    {
        var today = DateTimeOffset.UtcNow.Date;

        return new ActiveSessionsSummaryDto(
            rows.Where(row => row.Status == "Online").Select(row => row.UserId).Distinct().Count(),
            rows.Where(row => row.Status == "Idle").Select(row => row.UserId).Distinct().Count(),
            rows.Count(row => row.Status is "Online" or "Idle"),
            rows.Count(row => row.Suspicious),
            rows.Count(row => row.RevokedAt?.UtcDateTime.Date == today));
    }
}

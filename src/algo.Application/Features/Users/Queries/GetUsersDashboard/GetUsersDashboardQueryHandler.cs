using algo.Application.Abstractions;
using algo.Application.Common.AccessPolicy;
using algo.Application.Features.Users.Dtos;
using algo.Domain.Identity.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace algo.Application.Features.Users.Queries.GetUsersDashboard;

public sealed class GetUsersDashboardQueryHandler(
    IApplicationDbContext db,
    IAccessPolicyAuthorizationChecker authorizationChecker,
    IAccessPolicyQueryFilter queryFilter) : IRequestHandler<GetUsersDashboardQuery, UserDashboardStatsDto>
{
    public async Task<UserDashboardStatsDto> Handle(GetUsersDashboardQuery request, CancellationToken cancellationToken)
    {
        await authorizationChecker.EnsureResourceActionAllowedAsync(
            db,
            AccessPolicyResources.Users,
            AccessPolicyActions.Read,
            cancellationToken);

        var utc = DateTimeOffset.UtcNow;
        var startOfToday = new DateTimeOffset(utc.Date, TimeSpan.Zero);
        var startOfWeek = startOfToday.AddDays(-(int)utc.DayOfWeek);
        var startOfMonth = new DateTimeOffset(utc.Year, utc.Month, 1, 0, 0, 0, TimeSpan.Zero);

        IQueryable<ApplicationUser> baseQuery = db.Users.AsNoTracking();
        baseQuery = await queryFilter.ApplyAsync(
            baseQuery,
            AccessPolicyResources.Users,
            AccessPolicyActions.Read,
            cancellationToken);

        var total = await baseQuery.CountAsync(cancellationToken);
        var active = await baseQuery.CountAsync(u => u.IsActive, cancellationToken);
        var locked = await baseQuery.CountAsync(u => u.LockoutEnd.HasValue && u.LockoutEnd > utc, cancellationToken);
        var emailConfirmed = await baseQuery.CountAsync(u => u.EmailConfirmed, cancellationToken);
        var phoneConfirmed = await baseQuery.CountAsync(u => u.PhoneNumberConfirmed, cancellationToken);

        var newToday = await baseQuery.CountAsync(u => u.CreatedAt >= startOfToday, cancellationToken);
        var newWeek = await baseQuery.CountAsync(u => u.CreatedAt >= startOfWeek, cancellationToken);
        var newMonth = await baseQuery.CountAsync(u => u.CreatedAt >= startOfMonth, cancellationToken);

        var byRolePairs = await (
                from u in baseQuery
                join ur in db.UserRoles on u.Id equals ur.UserId
                join r in db.Roles on ur.RoleId equals r.Id
                group u by r.Name
                into g
                select new { Role = g.Key!, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var byRole = byRolePairs.ToDictionary(x => x.Role, x => x.Count, StringComparer.Ordinal);

        var recentUsers = await baseQuery
            .OrderByDescending(u => u.CreatedAt)
            .Take(10)
            .Select(u => new UserActivityDto(u.Id, u.Email, u.DisplayName, u.CreatedAt, "Registered"))
            .ToListAsync(cancellationToken);

        var recentlyLocked = await baseQuery
            .Where(u => u.LockoutEnd.HasValue && u.LockoutEnd > utc)
            .OrderByDescending(u => u.LockoutEnd)
            .Take(10)
            .Select(u => new UserActivityDto(u.Id, u.Email, u.DisplayName, u.LockoutEnd!.Value, "Locked"))
            .ToListAsync(cancellationToken);

        return new UserDashboardStatsDto(
            TotalUsers: total,
            ActiveUsers: active,
            InactiveUsers: total - active,
            LockedUsers: locked,
            EmailConfirmedUsers: emailConfirmed,
            EmailNotConfirmedUsers: total - emailConfirmed,
            PhoneConfirmedUsers: phoneConfirmed,
            NewUsersToday: newToday,
            NewUsersThisWeek: newWeek,
            NewUsersThisMonth: newMonth,
            UsersByRole: byRole,
            RecentUsers: recentUsers,
            RecentlyLockedUsers: recentlyLocked);
    }
}

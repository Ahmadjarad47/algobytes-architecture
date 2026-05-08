using algo.Domain.Identity.Entities;

namespace algo.Application.Common.Filtering;

public static class FilterExtensions
{
    public static IQueryable<ApplicationUser> ApplyUserFilters(
        this IQueryable<ApplicationUser> query,
        FilterRequest filters,
        DateTimeOffset utcNow)
    {
        if (filters.IsActive is { } active)
            query = query.Where(u => u.IsActive == active);

        if (filters.IsLocked is { } locked)
        {
            query = locked
                ? query.Where(u => u.LockoutEnd.HasValue && u.LockoutEnd > utcNow)
                : query.Where(u => !u.LockoutEnd.HasValue || u.LockoutEnd <= utcNow);
        }

        if (filters.EmailConfirmed is { } emailConfirmed)
            query = query.Where(u => u.EmailConfirmed == emailConfirmed);

        if (filters.PhoneNumberConfirmed is { } phoneConfirmed)
            query = query.Where(u => u.PhoneNumberConfirmed == phoneConfirmed);

        if (filters.CreatedAt?.From is { } createdFrom)
            query = query.Where(u => u.CreatedAt >= createdFrom);

        if (filters.CreatedAt?.To is { } createdTo)
            query = query.Where(u => u.CreatedAt <= createdTo);

        if (filters.LastLoginAt?.From is { } loginFrom)
            query = query.Where(u => u.LastLoginAt.HasValue && u.LastLoginAt >= loginFrom);

        if (filters.LastLoginAt?.To is { } loginTo)
            query = query.Where(u => u.LastLoginAt.HasValue && u.LastLoginAt <= loginTo);

        return query;
    }
}

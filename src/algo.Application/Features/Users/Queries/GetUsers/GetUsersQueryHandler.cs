using algo.Application.Abstractions;
using algo.Application.Common.AccessPolicy;
using algo.Application.Common.Filtering;
using algo.Application.Common.Pagination;
using algo.Application.Features.Users.Dtos;
using algo.Application.Features.Users.Validation;
using algo.Domain.Identity.Entities;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL;

namespace algo.Application.Features.Users.Queries.GetUsers;

public sealed class GetUsersQueryHandler(
    IApplicationDbContext db,
    IAccessPolicyEvaluator accessPolicyEvaluator,
    RoleManager<IdentityRole> roleManager) : IRequestHandler<GetUsersQuery, PaginatedResult<UserListItemDto>>
{
    public async Task<PaginatedResult<UserListItemDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        await accessPolicyEvaluator.EnsureResourceActionAllowedAsync(
            db,
            AccessPolicyResources.Users,
            AccessPolicyActions.Read,
            cancellationToken);

        var utcNow = DateTimeOffset.UtcNow;
        var filters = request.Filters ?? new FilterRequest();

        IQueryable<ApplicationUser> query = db.Users.AsNoTracking();
        query = await accessPolicyEvaluator.ApplyAsync(
            query,
            AccessPolicyResources.Users,
            AccessPolicyActions.Read,
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = $"%{EscapeLike(request.Search.Trim())}%";
            query = query.Where(u =>
                (u.Email != null && EF.Functions.ILike(u.Email, pattern))
                || (u.UserName != null && EF.Functions.ILike(u.UserName, pattern))
                || EF.Functions.ILike(u.DisplayName, pattern)
                || (u.PhoneNumber != null && EF.Functions.ILike(u.PhoneNumber, pattern)));
        }

        query = query.ApplyUserFilters(filters, utcNow);

        if (!string.IsNullOrWhiteSpace(filters.RoleName))
        {
            var role = await roleManager.FindByNameAsync(filters.RoleName.Trim());
            if (role is null)
            {
                throw new ValidationException(new[]
                {
                    new ValidationFailure(nameof(FilterRequest.RoleName), "Role was not found."),
                });
            }

            var normalized = role.NormalizedName!;
            query =
                from u in query
                join ur in db.UserRoles on u.Id equals ur.UserId
                join r in db.Roles on ur.RoleId equals r.Id
                where r.NormalizedName == normalized
                select u;
        }

        query = ApplyUserSort(query, request.Sort, utcNow);

        var page = Math.Max(1, request.Pagination.PageNumber);
        var size = Math.Max(1, request.Pagination.PageSize);
        var total = await query.CountAsync(cancellationToken);
        var users = await query
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(cancellationToken);

        var userIds = users.Select(u => u.Id).ToList();
        var rolesByUser = await LoadRolesForUsersAsync(db, userIds, cancellationToken);
        var onlineUserIds = await LoadOnlineUserIdsAsync(db, userIds, utcNow, cancellationToken);

        var items = users.Select(u => new UserListItemDto(
            u.Id,
            u.Email,
            u.UserName,
            u.DisplayName,
            u.PhoneNumber,
            u.IsActive,
            u.LockoutEnd.HasValue && u.LockoutEnd > utcNow,
            u.EmailConfirmed,
            u.PhoneNumberConfirmed,
            u.CreatedAt,
            u.UpdatedAt,
            u.LastLoginAt,
            onlineUserIds.Contains(u.Id),
            rolesByUser.GetValueOrDefault(u.Id, []))).ToList();

        return new PaginatedResult<UserListItemDto>(items, page, size, total);
    }

    private static IQueryable<ApplicationUser> ApplyUserSort(
        IQueryable<ApplicationUser> query,
        SortRequest? sort,
        DateTimeOffset utcNow)
    {
        var field = sort?.Field?.Trim();
        if (string.IsNullOrEmpty(field))
            return query.OrderBy(u => u.Email);

        var desc = sort?.Direction == Common.Sorting.SortDirection.Descending;

        return field.Equals(UserSortFields.Email, StringComparison.OrdinalIgnoreCase)
            ? desc ? query.OrderByDescending(u => u.Email) : query.OrderBy(u => u.Email)
            : field.Equals(UserSortFields.DisplayName, StringComparison.OrdinalIgnoreCase)
                ? desc
                    ? query.OrderByDescending(u => u.DisplayName)
                    : query.OrderBy(u => u.DisplayName)
                : field.Equals(UserSortFields.CreatedAt, StringComparison.OrdinalIgnoreCase)
                    ? desc
                        ? query.OrderByDescending(u => u.CreatedAt)
                        : query.OrderBy(u => u.CreatedAt)
                    : field.Equals(UserSortFields.LastLoginAt, StringComparison.OrdinalIgnoreCase)
                        ? desc
                            ? query.OrderByDescending(u => u.LastLoginAt)
                            : query.OrderBy(u => u.LastLoginAt)
                        : field.Equals(UserSortFields.Status, StringComparison.OrdinalIgnoreCase)
                            ? desc
                                ? query.OrderByDescending(u => u.LockoutEnd.HasValue && u.LockoutEnd > utcNow)
                                    .ThenByDescending(u => u.IsActive)
                                    .ThenBy(u => u.Email)
                                : query.OrderBy(u => u.LockoutEnd.HasValue && u.LockoutEnd > utcNow)
                                    .ThenBy(u => u.IsActive)
                                    .ThenBy(u => u.Email)
                            : query.OrderBy(u => u.Email);
    }

    private static async Task<Dictionary<string, string[]>> LoadRolesForUsersAsync(
        IApplicationDbContext db,
        List<string> userIds,
        CancellationToken cancellationToken)
    {
        if (userIds.Count == 0)
            return new Dictionary<string, string[]>(StringComparer.Ordinal);

        var pairs = await (
                from ur in db.UserRoles
                join r in db.Roles on ur.RoleId equals r.Id
                where userIds.Contains(ur.UserId)
                select new { ur.UserId, RoleName = r.Name! })
            .ToListAsync(cancellationToken);

        return pairs
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.RoleName).Distinct().OrderBy(n => n).ToArray(), StringComparer.Ordinal);
    }

    private static async Task<HashSet<string>> LoadOnlineUserIdsAsync(
        IApplicationDbContext db,
        List<string> userIds,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        if (userIds.Count == 0)
            return [];

        var ids = await db.RefreshTokens
            .AsNoTracking()
            .Where(token =>
                userIds.Contains(token.UserId) &&
                token.RevokedAt == null &&
                token.ExpiresAt > utcNow)
            .Select(token => token.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return ids.ToHashSet(StringComparer.Ordinal);
    }

    private static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
}

using algo.Application.Abstractions;
using algo.Application.Common.AccessPolicy;
using algo.Application.Common.CustomFields;
using algo.Application.Common.Filtering;
using algo.Application.Common.Pagination;
using algo.Application.Features.Users.Dtos;
using algo.Application.Features.Users.Validation;
using algo.Domain.CustomFields;
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
    IAccessPolicyAuthorizationChecker authorizationChecker,
    IAccessPolicyQueryFilter queryFilter,
    RoleManager<ApplicationRole> roleManager) : IRequestHandler<GetUsersQuery, PaginatedResult<UserListItemDto>>
{
    public async Task<PaginatedResult<UserListItemDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        await authorizationChecker.EnsureResourceActionAllowedAsync(
            db,
            AccessPolicyResources.Users,
            AccessPolicyActions.Read,
            cancellationToken);

        var utcNow = DateTimeOffset.UtcNow;
        var filters = request.Filters ?? new FilterRequest();
        var customFieldDefinitions = await LoadCustomFieldDefinitionsAsync(db, request, cancellationToken);
        var usePostgresCustomFields = SupportsPostgresJsonb(db) && customFieldDefinitions.Count > 0;

        IQueryable<ApplicationUser> query = request.IncludeTrashed || request.OnlyTrashed
            ? db.Users.IgnoreQueryFilters().AsNoTracking()
            : db.Users.AsNoTracking();
        query = await queryFilter.ApplyAsync(
            query,
            AccessPolicyResources.Users,
            AccessPolicyActions.Read,
            cancellationToken);

        if (request.OnlyTrashed)
        {
            query = query.Where(user => user.TrashedAt != null && user.DeletedAt == null);
        }
        else if (!request.IncludeTrashed)
        {
            query = query.Where(user => user.TrashedAt == null && user.DeletedAt == null);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            query = ApplySearch(query, request.Search.Trim(), customFieldDefinitions, usePostgresCustomFields);
        }

        query = query.ApplyUserFilters(filters, utcNow);
        query = ApplyCustomFieldFilters(query, request.CustomFieldFilters, customFieldDefinitions, usePostgresCustomFields);

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

        query = ApplyUserSort(query, request.Sort, utcNow, customFieldDefinitions, usePostgresCustomFields);

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
            u.TrashedAt,
            u.TrashExpiresAt,
            u.DeletedAt,
            JsonDocumentHelpers.CloneToElement(u.CustomFields),
            onlineUserIds.Contains(u.Id),
            u.TwoFactorEnabled,
            u.TotpRequiredByAdmin,
            rolesByUser.GetValueOrDefault(u.Id, []))).ToList();

        return new PaginatedResult<UserListItemDto>(items, page, size, total);
    }

    private static IQueryable<ApplicationUser> ApplyUserSort(
        IQueryable<ApplicationUser> query,
        SortRequest? sort,
        DateTimeOffset utcNow,
        IReadOnlyList<CustomFieldDefinition> customFieldDefinitions,
        bool usePostgresCustomFields)
    {
        var field = sort?.Field?.Trim();
        if (string.IsNullOrEmpty(field))
            return query.OrderBy(u => u.Email);

        var desc = sort?.Direction == Common.Sorting.SortDirection.Descending;
        var customField = ResolveCustomField(field, customFieldDefinitions, sortableOnly: true);
        if (usePostgresCustomFields && customField is not null)
        {
            return ApplyCustomFieldSort(query, customField, desc);
        }

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

    private static IQueryable<ApplicationUser> ApplySearch(
        IQueryable<ApplicationUser> query,
        string search,
        IReadOnlyList<CustomFieldDefinition> customFieldDefinitions,
        bool usePostgresCustomFields)
    {
        var pattern = $"%{EscapeLike(search)}%";
        var searchQuery = query.Where(u =>
            (u.Email != null && EF.Functions.ILike(u.Email, pattern))
            || (u.UserName != null && EF.Functions.ILike(u.UserName, pattern))
            || EF.Functions.ILike(u.DisplayName, pattern)
            || (u.PhoneNumber != null && EF.Functions.ILike(u.PhoneNumber, pattern)));

        if (!usePostgresCustomFields)
        {
            return searchQuery;
        }

        foreach (var definition in customFieldDefinitions.Where(definition => definition.Searchable))
        {
            searchQuery = definition.Type switch
            {
                CustomFieldType.Text or CustomFieldType.Select or CustomFieldType.Date =>
                    searchQuery.Union(query.Where(user =>
                        user.CustomFields != null &&
                        EF.Functions.ILike(user.CustomFields.RootElement.GetProperty(definition.Key).GetString()!, pattern))),
                _ => searchQuery
            };
        }

        return searchQuery;
    }

    private static IQueryable<ApplicationUser> ApplyCustomFieldFilters(
        IQueryable<ApplicationUser> query,
        IReadOnlyDictionary<string, string?>? customFieldFilters,
        IReadOnlyList<CustomFieldDefinition> customFieldDefinitions,
        bool usePostgresCustomFields)
    {
        if (!usePostgresCustomFields || customFieldFilters is null || customFieldFilters.Count == 0)
        {
            return query;
        }

        foreach (var (field, rawValue) in customFieldFilters)
        {
            var definition = ResolveCustomField(field, customFieldDefinitions, sortableOnly: false);
            if (definition is null || !definition.Filterable || string.IsNullOrWhiteSpace(rawValue))
            {
                continue;
            }

            query = definition.Type switch
            {
                CustomFieldType.Text or CustomFieldType.Select or CustomFieldType.Date =>
                    query.Where(user =>
                        user.CustomFields != null &&
                        EF.Functions.ILike(
                            user.CustomFields.RootElement.GetProperty(definition.Key).GetString()!,
                            $"%{EscapeLike(rawValue.Trim())}%")),
                CustomFieldType.Boolean when bool.TryParse(rawValue, out var boolValue) =>
                    query.Where(user =>
                        user.CustomFields != null &&
                        user.CustomFields.RootElement.GetProperty(definition.Key).GetBoolean() == boolValue),
                CustomFieldType.Number when decimal.TryParse(rawValue, out var numberValue) =>
                    query.Where(user =>
                        user.CustomFields != null &&
                        user.CustomFields.RootElement.GetProperty(definition.Key).GetDecimal() == numberValue),
                _ => query
            };
        }

        return query;
    }

    private static IQueryable<ApplicationUser> ApplyCustomFieldSort(
        IQueryable<ApplicationUser> query,
        CustomFieldDefinition definition,
        bool desc)
    {
        return definition.Type switch
        {
            CustomFieldType.Number => desc
                ? query.OrderByDescending(user => user.CustomFields == null ? (decimal?)null : user.CustomFields.RootElement.GetProperty(definition.Key).GetDecimal())
                : query.OrderBy(user => user.CustomFields == null ? (decimal?)null : user.CustomFields.RootElement.GetProperty(definition.Key).GetDecimal()),
            CustomFieldType.Boolean => desc
                ? query.OrderByDescending(user => user.CustomFields != null && user.CustomFields.RootElement.GetProperty(definition.Key).GetBoolean())
                : query.OrderBy(user => user.CustomFields != null && user.CustomFields.RootElement.GetProperty(definition.Key).GetBoolean()),
            _ => desc
                ? query.OrderByDescending(user => user.CustomFields == null ? null : user.CustomFields.RootElement.GetProperty(definition.Key).GetString())
                : query.OrderBy(user => user.CustomFields == null ? null : user.CustomFields.RootElement.GetProperty(definition.Key).GetString())
        };
    }

    private static CustomFieldDefinition? ResolveCustomField(
        string field,
        IReadOnlyList<CustomFieldDefinition> customFieldDefinitions,
        bool sortableOnly)
    {
        if (!field.StartsWith("customFields.", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var key = field["customFields.".Length..];
        return customFieldDefinitions.FirstOrDefault(definition =>
            string.Equals(definition.Key, key, StringComparison.OrdinalIgnoreCase)
            && (!sortableOnly || definition.Sortable));
    }

    private static bool SupportsPostgresJsonb(IApplicationDbContext db) =>
        db is DbContext context
        && string.Equals(context.Database.ProviderName, "Npgsql.EntityFrameworkCore.PostgreSQL", StringComparison.Ordinal);

    private static async Task<IReadOnlyList<CustomFieldDefinition>> LoadCustomFieldDefinitionsAsync(
        IApplicationDbContext db,
        GetUsersQuery request,
        CancellationToken cancellationToken)
    {
        var sortField = request.Sort?.Field?.Trim();
        var needsDefinitions =
            !string.IsNullOrWhiteSpace(request.Search)
            || (request.CustomFieldFilters?.Count > 0)
            || (!string.IsNullOrWhiteSpace(sortField)
                && sortField.StartsWith("customFields.", StringComparison.OrdinalIgnoreCase));

        if (!needsDefinitions)
        {
            return [];
        }

        return await db.CustomFieldDefinitions
            .AsNoTracking()
            .Where(definition => definition.Entity == CustomFieldEntities.Users)
            .ToListAsync(cancellationToken);
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

        var refreshTokensQuery = db.RefreshTokens
            .AsNoTracking()
            .Where(token => userIds.Contains(token.UserId) && token.RevokedAt == null);

        List<string> ids;
        if (db is DbContext context && string.Equals(context.Database.ProviderName, "Microsoft.EntityFrameworkCore.Sqlite", StringComparison.Ordinal))
        {
            ids = (await refreshTokensQuery.ToListAsync(cancellationToken))
                .Where(token => token.ExpiresAt > utcNow)
                .Select(token => token.UserId)
                .Distinct()
                .ToList();
        }
        else
        {
            ids = await refreshTokensQuery
                .Where(token => token.ExpiresAt > utcNow)
                .Select(token => token.UserId)
                .Distinct()
                .ToListAsync(cancellationToken);
        }

        return ids.ToHashSet(StringComparer.Ordinal);
    }

    private static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
}

using algo.Application.Abstractions;
using algo.Application.Common.AccessPolicy;
using algo.Application.Common.CustomFields;
using algo.Application.Features.Users.Dtos;
using algo.Domain.Identity.Entities;
using algo.Domain.Identity.Policies;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace algo.Application.Features.Users.Queries.SearchUsers;

public sealed class SearchUsersQueryHandler(
    IApplicationDbContext db,
    IAccessPolicyAuthorizationChecker authorizationChecker,
    IAccessPolicyQueryFilter queryFilter,
    RoleManager<ApplicationRole> roleManager) : IRequestHandler<SearchUsersQuery, SearchUsersResponseDto>
{
    public async Task<SearchUsersResponseDto> Handle(SearchUsersQuery request, CancellationToken cancellationToken)
    {
        await authorizationChecker.EnsureResourceActionAllowedAsync(db, AccessPolicyResources.Users, AccessPolicyActions.Read, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var includes = (request.Include ?? []).Select(x => x.Trim()).Where(x => x.Length > 0).ToHashSet(StringComparer.OrdinalIgnoreCase);

        IQueryable<ApplicationUser> query = db.Users.AsNoTracking();
        query = await queryFilter.ApplyAsync(query, AccessPolicyResources.Users, AccessPolicyActions.Read, cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = $"%{EscapeLike(request.Search.Trim())}%";
            query = query.Where(u =>
                (u.Email != null && EF.Functions.ILike(u.Email, pattern)) ||
                (u.UserName != null && EF.Functions.ILike(u.UserName, pattern)) ||
                EF.Functions.ILike(u.DisplayName, pattern) ||
                (u.PhoneNumber != null && EF.Functions.ILike(u.PhoneNumber, pattern)));
        }

        foreach (var f in request.Filters ?? [])
            query = await ApplyFilterAsync(query, f, now, cancellationToken);

        query = ApplySorts(query, request.Sort ?? [], now);

        var page = Math.Max(1, request.Page);
        var limit = Math.Clamp(request.Limit, 1, 100);
        var total = await query.CountAsync(cancellationToken);
        var users = await query.Skip((page - 1) * limit).Take(limit).ToListAsync(cancellationToken);

        var userIds = users.Select(u => u.Id).ToList();
        var rolesByUser = includes.Contains("roles") || includes.Contains("permissions")
            ? await LoadRolesForUsersAsync(userIds, cancellationToken)
            : new Dictionary<string, string[]>(StringComparer.Ordinal);

        var permsByUser = includes.Contains("permissions")
            ? await LoadPermissionsForUsersAsync(rolesByUser, cancellationToken)
            : new Dictionary<string, string[]>(StringComparer.Ordinal);

        var online = await LoadOnlineUserIdsAsync(userIds, now, cancellationToken);

        var items = users.Select(u => new UserListItemDto(
            u.Id, u.Email, u.UserName, u.DisplayName, u.PhoneNumber,
            u.IsActive, u.LockoutEnd.HasValue && u.LockoutEnd > now, u.EmailConfirmed, u.PhoneNumberConfirmed,
            u.CreatedAt, u.UpdatedAt, u.LastLoginAt, u.TrashedAt, u.TrashExpiresAt, u.DeletedAt, JsonDocumentHelpers.CloneToElement(u.CustomFields), online.Contains(u.Id), u.TwoFactorEnabled, u.TotpRequiredByAdmin,
            includes.Contains("roles") || includes.Contains("permissions") ? rolesByUser.GetValueOrDefault(u.Id, []) : [],
            includes.Contains("permissions") ? permsByUser.GetValueOrDefault(u.Id, []) : null)).ToList();

        return new SearchUsersResponseDto(
            items,
            new SearchUsersPaginationDto(page, limit, total, (int)Math.Ceiling(total / (double)limit)),
            includes.OrderBy(x => x).ToArray());
    }

    private async Task<IQueryable<ApplicationUser>> ApplyFilterAsync(IQueryable<ApplicationUser> query, SearchUsersFilterDto filter, DateTimeOffset now, CancellationToken ct)
    {
        var field = filter.Field.Trim().ToLowerInvariant();
        var op = filter.Operator.Trim().ToLowerInvariant();

        return field switch
        {
            "email" => ApplyString(query, op, filter, u => u.Email),
            "username" => ApplyString(query, op, filter, u => u.UserName),
            "displayname" => ApplyString(query, op, filter, u => u.DisplayName),
            "phonenumber" => ApplyString(query, op, filter, u => u.PhoneNumber),
            "isactive" => ApplyBool(query, op, filter, u => u.IsActive),
            "emailconfirmed" => ApplyBool(query, op, filter, u => u.EmailConfirmed),
            "phonenumberconfirmed" => ApplyBool(query, op, filter, u => u.PhoneNumberConfirmed),
            "createdat" => ApplyDate(query, op, filter, u => u.CreatedAt),
            "updatedat" => ApplyDate(query, op, filter, u => u.UpdatedAt),
            "lastloginat" => ApplyNullableDate(query, op, filter, u => u.LastLoginAt),
            "status" => ApplyStatus(query, op, filter, now),
            "role" => await ApplyRoleAsync(query, op, filter, ct),
            _ => throw BuildValidationException($"Field '{filter.Field}' is not supported.")
        };
    }

    private static IQueryable<ApplicationUser> ApplyString(IQueryable<ApplicationUser> query, string op, SearchUsersFilterDto f, System.Linq.Expressions.Expression<Func<ApplicationUser, string?>> expr)
    {
        var v = op is "isnull" or "isnotnull" ? null : RequireString(f);
        var p = v is null ? null : $"%{EscapeLike(v)}%";
        var sw = v is null ? null : $"{EscapeLike(v)}%";
        var ew = v is null ? null : $"%{EscapeLike(v)}";

        if (expr.Body.ToString().EndsWith(".Email"))
            return ApplyStringCore(query, op, f, v, p, sw, ew, u => u.Email);
        if (expr.Body.ToString().EndsWith(".UserName"))
            return ApplyStringCore(query, op, f, v, p, sw, ew, u => u.UserName);
        if (expr.Body.ToString().EndsWith(".DisplayName"))
            return ApplyStringCore(query, op, f, v, p, sw, ew, u => u.DisplayName);
        return ApplyStringCore(query, op, f, v, p, sw, ew, u => u.PhoneNumber);
    }

    private static IQueryable<ApplicationUser> ApplyStringCore(IQueryable<ApplicationUser> query, string op, SearchUsersFilterDto f, string? value, string? contains, string? starts, string? ends, System.Linq.Expressions.Expression<Func<ApplicationUser, string?>> sel)
    {
        return op switch
        {
            "eq" => query.Where(Replace(sel, s => s == value!)),
            "ne" => query.Where(Replace(sel, s => s != value!)),
            "contains" => query.Where(Replace(sel, s => s != null && EF.Functions.ILike(s, contains!))),
            "startswith" => query.Where(Replace(sel, s => s != null && EF.Functions.ILike(s, starts!))),
            "endswith" => query.Where(Replace(sel, s => s != null && EF.Functions.ILike(s, ends!))),
            "in" => query.Where(Replace(sel, s => ReadStringArray(f).Contains(s!))),
            "nin" => query.Where(Replace(sel, s => !ReadStringArray(f).Contains(s!))),
            "isnull" => query.Where(Replace(sel, s => s == null)),
            "isnotnull" => query.Where(Replace(sel, s => s != null)),
            _ => throw BuildValidationException($"Operator '{op}' is not valid for string fields.")
        };
    }

    private static IQueryable<ApplicationUser> ApplyBool(IQueryable<ApplicationUser> query, string op, SearchUsersFilterDto f, System.Linq.Expressions.Expression<Func<ApplicationUser, bool>> sel)
    {
        var v = RequireBool(f);
        return op switch
        {
            "eq" => query.Where(Replace(sel, b => b == v)),
            "ne" => query.Where(Replace(sel, b => b != v)),
            _ => throw BuildValidationException($"Operator '{op}' is not valid for bool fields.")
        };
    }

    private static IQueryable<ApplicationUser> ApplyDate(IQueryable<ApplicationUser> query, string op, SearchUsersFilterDto f, System.Linq.Expressions.Expression<Func<ApplicationUser, DateTimeOffset>> sel)
    {
        var v = RequireDate(f);
        return op switch
        {
            "eq" => query.Where(Replace(sel, d => d == v)),
            "ne" => query.Where(Replace(sel, d => d != v)),
            "gt" => query.Where(Replace(sel, d => d > v)),
            "gte" => query.Where(Replace(sel, d => d >= v)),
            "lt" => query.Where(Replace(sel, d => d < v)),
            "lte" => query.Where(Replace(sel, d => d <= v)),
            _ => throw BuildValidationException($"Operator '{op}' is not valid for date fields.")
        };
    }

    private static IQueryable<ApplicationUser> ApplyNullableDate(IQueryable<ApplicationUser> query, string op, SearchUsersFilterDto f, System.Linq.Expressions.Expression<Func<ApplicationUser, DateTimeOffset?>> sel)
    {
        var v = op is "isnull" or "isnotnull" ? (DateTimeOffset?)null : RequireDate(f);
        return op switch
        {
            "isnull" => query.Where(Replace(sel, d => d == null)),
            "isnotnull" => query.Where(Replace(sel, d => d != null)),
            "eq" => query.Where(Replace(sel, d => d != null && d == v)),
            "ne" => query.Where(Replace(sel, d => d == null || d != v)),
            "gt" => query.Where(Replace(sel, d => d != null && d > v)),
            "gte" => query.Where(Replace(sel, d => d != null && d >= v)),
            "lt" => query.Where(Replace(sel, d => d != null && d < v)),
            "lte" => query.Where(Replace(sel, d => d != null && d <= v)),
            _ => throw BuildValidationException($"Operator '{op}' is not valid for nullable date fields.")
        };
    }

    private static IQueryable<ApplicationUser> ApplyStatus(IQueryable<ApplicationUser> query, string op, SearchUsersFilterDto f, DateTimeOffset now)
    {
        var vals = op is "in" or "nin" ? ReadStringArray(f) : [RequireString(f)];
        var s = vals.Select(x => x.Trim().ToLowerInvariant()).ToHashSet();
        if (s.Any(x => x is not ("active" or "inactive" or "locked")))
            throw BuildValidationException("Status enum supports: active, inactive, locked.");

        var selected = query.Where(u =>
            (s.Contains("active") && u.IsActive && (!u.LockoutEnd.HasValue || u.LockoutEnd <= now)) ||
            (s.Contains("inactive") && !u.IsActive && (!u.LockoutEnd.HasValue || u.LockoutEnd <= now)) ||
            (s.Contains("locked") && u.LockoutEnd.HasValue && u.LockoutEnd > now));

        return op is "ne" or "nin" ? query.Except(selected) : selected;
    }

    private async Task<IQueryable<ApplicationUser>> ApplyRoleAsync(IQueryable<ApplicationUser> query, string op, SearchUsersFilterDto f, CancellationToken ct)
    {
        var names = op is "in" or "nin" ? ReadStringArray(f) : [RequireString(f)];
        var normalized = new List<string>();
        foreach (var name in names)
        {
            var role = await roleManager.FindByNameAsync(name.Trim());
            if (role is null) throw new ValidationException([new ValidationFailure("filters.role", $"Role '{name}' was not found.")]);
            normalized.Add(role.NormalizedName!);
        }

        var selected = from u in query
                       join ur in db.UserRoles on u.Id equals ur.UserId
                       join r in db.Roles on ur.RoleId equals r.Id
                       where normalized.Contains(r.NormalizedName!)
                       select u;

        return op is "ne" or "nin" ? query.Except(selected).Distinct() : selected.Distinct();
    }

    private static IQueryable<ApplicationUser> ApplySorts(IQueryable<ApplicationUser> q, IReadOnlyList<SearchUsersSortDto> sorts, DateTimeOffset now)
    {
        if (sorts.Count == 0) return q.OrderByDescending(x => x.CreatedAt);
        IOrderedQueryable<ApplicationUser>? o = null;
        foreach (var s in sorts)
        {
            var f = s.Field.Trim().ToLowerInvariant();
            var d = s.Direction.Trim().Equals("desc", StringComparison.OrdinalIgnoreCase);
            o = (f, d) switch
            {
                ("email", false) => o is null ? q.OrderBy(x => x.Email) : o.ThenBy(x => x.Email),
                ("email", true) => o is null ? q.OrderByDescending(x => x.Email) : o.ThenByDescending(x => x.Email),
                ("username", false) => o is null ? q.OrderBy(x => x.UserName) : o.ThenBy(x => x.UserName),
                ("username", true) => o is null ? q.OrderByDescending(x => x.UserName) : o.ThenByDescending(x => x.UserName),
                ("displayname", false) => o is null ? q.OrderBy(x => x.DisplayName) : o.ThenBy(x => x.DisplayName),
                ("displayname", true) => o is null ? q.OrderByDescending(x => x.DisplayName) : o.ThenByDescending(x => x.DisplayName),
                ("createdat", false) => o is null ? q.OrderBy(x => x.CreatedAt) : o.ThenBy(x => x.CreatedAt),
                ("createdat", true) => o is null ? q.OrderByDescending(x => x.CreatedAt) : o.ThenByDescending(x => x.CreatedAt),
                ("updatedat", false) => o is null ? q.OrderBy(x => x.UpdatedAt) : o.ThenBy(x => x.UpdatedAt),
                ("updatedat", true) => o is null ? q.OrderByDescending(x => x.UpdatedAt) : o.ThenByDescending(x => x.UpdatedAt),
                ("lastloginat", false) => o is null ? q.OrderBy(x => x.LastLoginAt) : o.ThenBy(x => x.LastLoginAt),
                ("lastloginat", true) => o is null ? q.OrderByDescending(x => x.LastLoginAt) : o.ThenByDescending(x => x.LastLoginAt),
                ("status", false) => o is null ? q.OrderBy(x => x.LockoutEnd.HasValue && x.LockoutEnd > now).ThenBy(x => x.IsActive) : o.ThenBy(x => x.LockoutEnd.HasValue && x.LockoutEnd > now).ThenBy(x => x.IsActive),
                ("status", true) => o is null ? q.OrderByDescending(x => x.LockoutEnd.HasValue && x.LockoutEnd > now).ThenByDescending(x => x.IsActive) : o.ThenByDescending(x => x.LockoutEnd.HasValue && x.LockoutEnd > now).ThenByDescending(x => x.IsActive),
                _ => o
            };
        }
        return o ?? q.OrderByDescending(x => x.CreatedAt);
    }

    private static string RequireString(SearchUsersFilterDto f) =>
        f.Value.ValueKind == System.Text.Json.JsonValueKind.String ? f.Value.GetString() ?? string.Empty : throw BuildValidationException($"Field '{f.Field}' expects string.");

    private static bool RequireBool(SearchUsersFilterDto f) =>
        f.Value.ValueKind is System.Text.Json.JsonValueKind.True or System.Text.Json.JsonValueKind.False ? f.Value.GetBoolean() : throw BuildValidationException($"Field '{f.Field}' expects bool.");

    private static DateTimeOffset RequireDate(SearchUsersFilterDto f) =>
        f.Value.ValueKind == System.Text.Json.JsonValueKind.String && DateTimeOffset.TryParse(f.Value.GetString(), out var d) ? d : throw BuildValidationException($"Field '{f.Field}' expects ISO date.");

    private static List<string> ReadStringArray(SearchUsersFilterDto f)
    {
        if (f.Value.ValueKind != System.Text.Json.JsonValueKind.Array) throw BuildValidationException($"Field '{f.Field}' expects array.");
        return f.Value.EnumerateArray().Select(x => x.ValueKind == System.Text.Json.JsonValueKind.String ? x.GetString() ?? string.Empty : throw BuildValidationException($"Field '{f.Field}' array expects strings.")).ToList();
    }

    private static ValidationException BuildValidationException(string msg) => new([new ValidationFailure("dynamicQuery", msg)]);

    private async Task<Dictionary<string, string[]>> LoadRolesForUsersAsync(List<string> userIds, CancellationToken ct)
    {
        if (userIds.Count == 0) return new Dictionary<string, string[]>(StringComparer.Ordinal);
        var pairs = await (from ur in db.UserRoles join r in db.Roles on ur.RoleId equals r.Id where userIds.Contains(ur.UserId) select new { ur.UserId, RoleName = r.Name! }).ToListAsync(ct);
        return pairs.GroupBy(x => x.UserId).ToDictionary(g => g.Key, g => g.Select(x => x.RoleName).Distinct().OrderBy(x => x).ToArray(), StringComparer.Ordinal);
    }

    private async Task<Dictionary<string, string[]>> LoadPermissionsForUsersAsync(Dictionary<string, string[]> rolesByUser, CancellationToken ct)
    {
        var allRoles = rolesByUser.Values.SelectMany(x => x).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (allRoles.Count == 0) return new Dictionary<string, string[]>(StringComparer.Ordinal);

        var rows = await db.AccessPolicies.AsNoTracking()
            .Where(p => p.IsEnabled && p.DeletedAt == null && p.SubjectType == AccessPolicySubjectType.Role && allRoles.Contains(p.SubjectKey) && p.Effect == AccessPolicyEffect.Allow)
            .Select(p => new { p.SubjectKey, Permission = p.Resource + ":" + p.Action })
            .ToListAsync(ct);

        var byRole = rows.GroupBy(x => x.SubjectKey).ToDictionary(g => g.Key, g => g.Select(x => x.Permission).Distinct().ToArray(), StringComparer.OrdinalIgnoreCase);
        return rolesByUser.ToDictionary(x => x.Key, x => x.Value.SelectMany(r => byRole.GetValueOrDefault(r, [])).Distinct().OrderBy(y => y).ToArray(), StringComparer.Ordinal);
    }

    private async Task<HashSet<string>> LoadOnlineUserIdsAsync(List<string> userIds, DateTimeOffset now, CancellationToken ct)
    {
        if (userIds.Count == 0) return [];
        var ids = await db.RefreshTokens.AsNoTracking().Where(t => userIds.Contains(t.UserId) && t.RevokedAt == null && t.ExpiresAt > now).Select(t => t.UserId).Distinct().ToListAsync(ct);
        return ids.ToHashSet(StringComparer.Ordinal);
    }

    private static string EscapeLike(string value) => value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("%", "\\%", StringComparison.Ordinal).Replace("_", "\\_", StringComparison.Ordinal);

    private static System.Linq.Expressions.Expression<Func<ApplicationUser, bool>> Replace<T>(System.Linq.Expressions.Expression<Func<ApplicationUser, T>> field, System.Linq.Expressions.Expression<Func<T, bool>> cond)
    {
        var param = field.Parameters[0];
        var body = new ReplaceVisitor(cond.Parameters[0], field.Body).Visit(cond.Body)!;
        return System.Linq.Expressions.Expression.Lambda<Func<ApplicationUser, bool>>(body, param);
    }

    private sealed class ReplaceVisitor(System.Linq.Expressions.Expression from, System.Linq.Expressions.Expression to) : System.Linq.Expressions.ExpressionVisitor
    {
        public override System.Linq.Expressions.Expression? Visit(System.Linq.Expressions.Expression? node) => node == from ? to : base.Visit(node);
    }
}

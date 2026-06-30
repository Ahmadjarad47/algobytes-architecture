using algo.Application.Abstractions;
using algo.Application.Common.AccessPolicy;
using algo.Application.Common.Pagination;
using algo.Application.Common.Sorting;
using algo.Application.Features.Logs.Dtos;
using algo.Domain.Logging.Entities;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace algo.Application.Features.Logs.Queries.GetLogs;

public sealed class GetLogsQueryHandler(
    IApplicationDbContext db,
    IAccessPolicyAuthorizationChecker authorizationChecker)
    : IRequestHandler<GetLogsQuery, PaginatedResult<ApplicationLogDto>>
{
    public async Task<PaginatedResult<ApplicationLogDto>> Handle(GetLogsQuery request, CancellationToken cancellationToken)
    {
        await authorizationChecker.EnsureResourceActionAllowedAsync(
            db,
            AccessPolicyResources.Logs,
            AccessPolicyActions.Read,
            cancellationToken);

        IQueryable<ApplicationLog> query = db.ApplicationLogs.AsNoTracking();

        var f = request.Filters;
        if (!string.IsNullOrWhiteSpace(f.Level))
        {
            var level = f.Level.Trim();
            query = query.Where(l => l.Level.ToLower() == level.ToLower());
        }

        if (f.FromTimestamp is { } from)
        {
            query = query.Where(l => l.Timestamp >= from);
        }

        if (f.ToTimestamp is { } to)
        {
            query = query.Where(l => l.Timestamp <= to);
        }

        if (!string.IsNullOrWhiteSpace(f.UserId))
        {
            var userId = f.UserId.Trim();
            query = query.Where(l => l.UserId == userId);
        }

        if (!string.IsNullOrWhiteSpace(f.UserName))
        {
            var pattern = $"%{EscapeLike(f.UserName.Trim())}%";
            query = query.Where(l => l.UserName != null && EF.Functions.ILike(l.UserName, pattern));
        }

        if (!string.IsNullOrWhiteSpace(f.TraceId))
        {
            var traceId = f.TraceId.Trim();
            query = query.Where(l => l.TraceId == traceId);
        }

        if (!string.IsNullOrWhiteSpace(f.RequestPath))
        {
            var pattern = $"%{EscapeLike(f.RequestPath.Trim())}%";
            query = query.Where(l => l.RequestPath != null && EF.Functions.ILike(l.RequestPath, pattern));
        }

        if (!string.IsNullOrWhiteSpace(f.RequestMethod))
        {
            var method = f.RequestMethod.Trim();
            query = query.Where(l => l.RequestMethod == method);
        }

        if (!string.IsNullOrWhiteSpace(f.MessageContains))
        {
            var pattern = $"%{EscapeLike(f.MessageContains.Trim())}%";
            query = query.Where(l => EF.Functions.ILike(l.Message, pattern));
        }

        var page = Math.Max(1, request.Pagination.PageNumber);
        var size = Math.Max(1, request.Pagination.PageSize);
        var desc = request.Sort.Direction == SortDirection.Descending;

        List<ApplicationLog> rows;
        int total;
        if (db is DbContext context && string.Equals(context.Database.ProviderName, "Microsoft.EntityFrameworkCore.Sqlite", StringComparison.Ordinal))
        {
            var materialized = await query.ToListAsync(cancellationToken);
            var ordered = desc
                ? materialized.OrderByDescending(log => log.Timestamp)
                : materialized.OrderBy(log => log.Timestamp);

            total = materialized.Count;
            rows = ordered
                .Skip((page - 1) * size)
                .Take(size)
                .ToList();
        }
        else
        {
            query = desc ? query.OrderByDescending(l => l.Timestamp) : query.OrderBy(l => l.Timestamp);
            total = await query.CountAsync(cancellationToken);
            rows = await query
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync(cancellationToken);
        }

        var items = rows.Select(r => r.Adapt<ApplicationLogDto>()).ToList();
        return new PaginatedResult<ApplicationLogDto>(items, page, size, total);
    }

    private static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
}

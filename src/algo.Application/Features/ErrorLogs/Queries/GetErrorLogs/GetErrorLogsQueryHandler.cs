using algo.Application.Abstractions;
using algo.Application.Common.AccessPolicy;
using algo.Application.Common.Pagination;
using algo.Application.Common.Sorting;
using algo.Application.Features.ErrorLogs.Dtos;
using algo.Domain.Logging.Entities;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace algo.Application.Features.ErrorLogs.Queries.GetErrorLogs;

public sealed class GetErrorLogsQueryHandler(
    IApplicationDbContext db,
    IAccessPolicyEvaluator accessPolicyEvaluator)
    : IRequestHandler<GetErrorLogsQuery, PaginatedResult<ErrorLogDto>>
{
    public async Task<PaginatedResult<ErrorLogDto>> Handle(
        GetErrorLogsQuery request,
        CancellationToken cancellationToken)
    {
        await accessPolicyEvaluator.EnsureResourceActionAllowedAsync(
            db,
            AccessPolicyResources.ErrorLogs,
            AccessPolicyActions.Read,
            cancellationToken);

        IQueryable<ErrorLog> query = db.ErrorLogs.AsNoTracking();

        var f = request.Filters;
        if (!string.IsNullOrWhiteSpace(f.ExceptionType))
        {
            var pattern = $"%{EscapeLike(f.ExceptionType.Trim())}%";
            query = query.Where(x => EF.Functions.ILike(x.ExceptionType, pattern));
        }

        if (f.StatusCode is { } statusCode)
        {
            query = query.Where(x => x.StatusCode == statusCode);
        }

        if (f.FromTimestamp is { } from)
        {
            query = query.Where(x => x.Timestamp >= from);
        }

        if (f.ToTimestamp is { } to)
        {
            query = query.Where(x => x.Timestamp <= to);
        }

        if (!string.IsNullOrWhiteSpace(f.UserId))
        {
            var userId = f.UserId.Trim();
            query = query.Where(x => x.UserId == userId);
        }

        if (!string.IsNullOrWhiteSpace(f.UserName))
        {
            var pattern = $"%{EscapeLike(f.UserName.Trim())}%";
            query = query.Where(x => x.UserName != null && EF.Functions.ILike(x.UserName, pattern));
        }

        if (!string.IsNullOrWhiteSpace(f.TraceId))
        {
            var traceId = f.TraceId.Trim();
            query = query.Where(x => x.TraceId == traceId);
        }

        if (!string.IsNullOrWhiteSpace(f.Path))
        {
            var pattern = $"%{EscapeLike(f.Path.Trim())}%";
            query = query.Where(x => x.Path != null && EF.Functions.ILike(x.Path, pattern));
        }

        if (!string.IsNullOrWhiteSpace(f.Method))
        {
            var method = f.Method.Trim();
            query = query.Where(x => x.Method == method);
        }

        if (!string.IsNullOrWhiteSpace(f.MessageContains))
        {
            var pattern = $"%{EscapeLike(f.MessageContains.Trim())}%";
            query = query.Where(x => EF.Functions.ILike(x.Message, pattern));
        }

        var page = Math.Max(1, request.Pagination.PageNumber);
        var size = Math.Max(1, request.Pagination.PageSize);
        var desc = request.Sort.Direction == SortDirection.Descending;

        List<ErrorLog> rows;
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
            query = desc ? query.OrderByDescending(x => x.Timestamp) : query.OrderBy(x => x.Timestamp);
            total = await query.CountAsync(cancellationToken);
            rows = await query
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync(cancellationToken);
        }

        var items = rows.Select(x => x.Adapt<ErrorLogDto>()).ToList();
        return new PaginatedResult<ErrorLogDto>(items, page, size, total);
    }

    private static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
}

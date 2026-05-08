using Microsoft.EntityFrameworkCore;

namespace algo.Application.Common.Pagination;

public static class PaginationExtensions
{
    public static IQueryable<T> ApplyPagination<T>(this IQueryable<T> query, PaginationRequest request)
    {
        var page = Math.Max(1, request.PageNumber);
        var size = Math.Max(1, request.PageSize);
        return query.Skip((page - 1) * size).Take(size);
    }

    public static async Task<PaginatedResult<T>> ToPaginatedResultAsync<T>(
        this IQueryable<T> query,
        PaginationRequest request,
        CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.PageNumber);
        var size = Math.Max(1, request.PageSize);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * size).Take(size).ToListAsync(cancellationToken);
        return new PaginatedResult<T>(items, page, size, total);
    }
}

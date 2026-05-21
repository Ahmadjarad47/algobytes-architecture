using algo.Application.Common.Filtering;
using algo.Application.Common.Pagination;
using algo.Application.Common.Sorting;
using algo.Application.Features.Logs.Dtos;

namespace algo.Application.Features.Logs.Queries.GetLogs;

/// <summary>Flat query binding for GET /api/logs; maps to <see cref="GetLogsQuery"/>.</summary>
public sealed record GetLogsQueryParameters(
    int PageNumber = 1,
    int PageSize = 20,
    string? Level = null,
    DateTimeOffset? FromTimestamp = null,
    DateTimeOffset? ToTimestamp = null,
    string? UserId = null,
    string? UserName = null,
    string? TraceId = null,
    string? RequestPath = null,
    string? RequestMethod = null,
    string? MessageContains = null,
    string? SortField = null,
    SortDirection SortDirection = SortDirection.Descending)
{
    public GetLogsQuery ToQuery() => new(
        new PaginationRequest(PageNumber, PageSize),
        new LogFilterDto(
            Level,
            FromTimestamp,
            ToTimestamp,
            UserId,
            UserName,
            TraceId,
            RequestPath,
            RequestMethod,
            MessageContains),
        new SortRequest(
            string.IsNullOrWhiteSpace(SortField) ? LogSortFields.Timestamp : SortField.Trim(),
            SortDirection));
}

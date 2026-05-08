using algo.Application.Common.Filtering;
using algo.Application.Common.Pagination;
using algo.Application.Common.Sorting;
using algo.Application.Features.ErrorLogs.Dtos;

namespace algo.Application.Features.ErrorLogs.Queries.GetErrorLogs;

public sealed record GetErrorLogsQueryParameters(
    int PageNumber = 1,
    int PageSize = 20,
    string? ExceptionType = null,
    int? StatusCode = null,
    DateTimeOffset? FromTimestamp = null,
    DateTimeOffset? ToTimestamp = null,
    string? UserId = null,
    string? UserName = null,
    string? TraceId = null,
    string? Path = null,
    string? Method = null,
    string? MessageContains = null,
    string? SortField = null,
    SortDirection SortDirection = SortDirection.Descending)
{
    public GetErrorLogsQuery ToQuery() => new(
        new PaginationRequest(PageNumber, PageSize),
        new ErrorLogFilterDto(
            ExceptionType,
            StatusCode,
            FromTimestamp,
            ToTimestamp,
            UserId,
            UserName,
            TraceId,
            Path,
            Method,
            MessageContains),
        new SortRequest(
            string.IsNullOrWhiteSpace(SortField) ? ErrorLogSortFields.Timestamp : SortField.Trim(),
            SortDirection));
}

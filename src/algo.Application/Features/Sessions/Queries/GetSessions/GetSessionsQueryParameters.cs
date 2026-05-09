using algo.Application.Common.Pagination;

namespace algo.Application.Features.Sessions.Queries.GetSessions;

public sealed record GetSessionsQueryParameters(
    int PageNumber = 1,
    int PageSize = 25,
    string? Search = null,
    string? Status = null,
    string? Role = null,
    string? Device = null,
    string? Browser = null,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    bool SuspiciousOnly = false)
{
    public GetSessionsQuery ToQuery() =>
        new(
            new PaginationRequest(PageNumber, PageSize),
            Search,
            Status,
            Role,
            Device,
            Browser,
            From,
            To,
            SuspiciousOnly);
}

using algo.Application.Common.Filtering;
using algo.Application.Common.Pagination;
using algo.Application.Common.Sorting;

namespace algo.Application.Features.Users.Queries.GetUsers;

/// <summary>Flat query binding model for GET /api/users; maps to <see cref="GetUsersQuery"/>.</summary>
public sealed record GetUsersQueryParameters(
    int PageNumber = 1,
    int PageSize = 20,
    string? Search = null,
    string? SortField = null,
    SortDirection SortDirection = SortDirection.Ascending,
    bool? IsActive = null,
    bool? IsLocked = null,
    bool? EmailConfirmed = null,
    bool? PhoneNumberConfirmed = null,
    string? RoleName = null,
    DateTimeOffset? CreatedFrom = null,
    DateTimeOffset? CreatedTo = null,
    DateTimeOffset? LastLoginFrom = null,
    DateTimeOffset? LastLoginTo = null)
{
    public GetUsersQuery ToQuery() => new(
        new PaginationRequest(PageNumber, PageSize),
        Search,
        new FilterRequest(
            IsActive,
            IsLocked,
            EmailConfirmed,
            PhoneNumberConfirmed,
            RoleName,
            CreatedFrom.HasValue || CreatedTo.HasValue ? new DateRangeFilter(CreatedFrom, CreatedTo) : null,
            LastLoginFrom.HasValue || LastLoginTo.HasValue ? new DateRangeFilter(LastLoginFrom, LastLoginTo) : null),
        string.IsNullOrWhiteSpace(SortField) ? null : new SortRequest(SortField, SortDirection));
}

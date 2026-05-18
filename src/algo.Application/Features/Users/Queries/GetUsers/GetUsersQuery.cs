using algo.Application.Common.Filtering;
using algo.Application.Common.Pagination;
using algo.Application.Features.Users.Dtos;
using MediatR;

namespace algo.Application.Features.Users.Queries.GetUsers;

public sealed record GetUsersQuery(
    PaginationRequest Pagination,
    string? Search,
    FilterRequest? Filters,
    SortRequest? Sort,
    IReadOnlyDictionary<string, string?>? CustomFieldFilters = null,
    bool IncludeTrashed = false,
    bool OnlyTrashed = false) : IRequest<PaginatedResult<UserListItemDto>>;

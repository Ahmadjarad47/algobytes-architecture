using System.Text.Json;
using algo.Application.Features.Users.Dtos;
using MediatR;

namespace algo.Application.Features.Users.Queries.SearchUsers;

public sealed record SearchUsersQuery(
    IReadOnlyList<SearchUsersFilterDto>? Filters,
    string? Search,
    IReadOnlyList<SearchUsersSortDto>? Sort,
    int Page = 1,
    int Limit = 20,
    IReadOnlyList<string>? Include = null) : IRequest<SearchUsersResponseDto>;

public sealed record SearchUsersFilterDto(string Field, string Operator, JsonElement Value);

public sealed record SearchUsersSortDto(string Field, string Direction);

public sealed record SearchUsersPaginationDto(int Page, int Limit, int Total, int TotalPages);

public sealed record SearchUsersResponseDto(
    IReadOnlyList<UserListItemDto> Items,
    SearchUsersPaginationDto Pagination,
    IReadOnlyList<string> Includes);

using algo.Application.Common.Pagination;
using algo.Application.Features.Sessions.Dtos;
using MediatR;

namespace algo.Application.Features.Sessions.Queries.GetSessions;

public sealed record GetSessionsQuery(
    PaginationRequest Pagination,
    string? Search,
    string? Status,
    string? Role,
    string? Device,
    string? Browser,
    DateTimeOffset? From,
    DateTimeOffset? To,
    bool SuspiciousOnly) : IRequest<ActiveSessionsResponseDto>;

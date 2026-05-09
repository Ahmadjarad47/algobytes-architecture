using algo.Application.Common.Pagination;

namespace algo.Application.Features.Sessions.Dtos;

public sealed record ActiveSessionsResponseDto(
    PaginatedResult<ActiveSessionDto> Sessions,
    ActiveSessionsSummaryDto Summary);

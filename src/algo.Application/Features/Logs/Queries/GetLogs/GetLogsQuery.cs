using algo.Application.Common.Filtering;
using algo.Application.Common.Pagination;
using algo.Application.Common.Sorting;
using algo.Application.Features.Logs.Dtos;
using MediatR;

namespace algo.Application.Features.Logs.Queries.GetLogs;

public sealed record GetLogsQuery(
    PaginationRequest Pagination,
    LogFilterDto Filters,
    SortRequest Sort) : IRequest<PaginatedResult<ApplicationLogDto>>;

using algo.Application.Common.Filtering;
using algo.Application.Common.Pagination;
using algo.Application.Features.ErrorLogs.Dtos;
using MediatR;

namespace algo.Application.Features.ErrorLogs.Queries.GetErrorLogs;

public sealed record GetErrorLogsQuery(
    PaginationRequest Pagination,
    ErrorLogFilterDto Filters,
    SortRequest Sort) : IRequest<PaginatedResult<ErrorLogDto>>;

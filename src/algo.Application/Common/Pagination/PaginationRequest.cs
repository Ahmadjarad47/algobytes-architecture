namespace algo.Application.Common.Pagination;

public sealed record PaginationRequest(int PageNumber = 1, int PageSize = 20);

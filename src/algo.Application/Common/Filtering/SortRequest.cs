using algo.Application.Common.Sorting;

namespace algo.Application.Common.Filtering;

public sealed record SortRequest(string? Field = null, SortDirection Direction = SortDirection.Ascending);

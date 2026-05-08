namespace algo.Application.Common.Filtering;

public sealed record DateRangeFilter(DateTimeOffset? From = null, DateTimeOffset? To = null);

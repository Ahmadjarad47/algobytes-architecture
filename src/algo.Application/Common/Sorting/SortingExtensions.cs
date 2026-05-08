namespace algo.Application.Common.Sorting;

public static class SortingExtensions
{
    public static string? NormalizeSortField(string? field) =>
        string.IsNullOrWhiteSpace(field) ? null : field.Trim();
}

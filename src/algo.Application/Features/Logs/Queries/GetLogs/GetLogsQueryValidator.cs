using algo.Application.Common.Sorting;
using FluentValidation;

namespace algo.Application.Features.Logs.Queries.GetLogs;

public sealed class GetLogsQueryValidator : AbstractValidator<GetLogsQuery>
{
    public GetLogsQueryValidator()
    {
        RuleFor(x => x.Pagination.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.Pagination.PageSize).InclusiveBetween(1, 100);

        RuleFor(x => x.Filters.FromTimestamp)
            .LessThanOrEqualTo(x => x.Filters.ToTimestamp)
            .When(x => x.Filters.FromTimestamp.HasValue && x.Filters.ToTimestamp.HasValue);

        RuleFor(x => x.Filters.Level)
            .Must(level => level is null || LogLevels.IsValid(level))
            .WithMessage("Level must be a valid Serilog level name.");

        RuleFor(x => x.Sort.Field)
            .Must(LogSortFields.IsSupported)
            .WithMessage($"Sort field must be '{LogSortFields.Timestamp}'.");
    }
}

internal static class LogLevels
{
    private static readonly HashSet<string> Known = new(StringComparer.OrdinalIgnoreCase)
    {
        "Verbose",
        "Debug",
        "Information",
        "Warning",
        "Error",
        "Fatal",
    };

    public static bool IsValid(string level) => Known.Contains(level.Trim());
}

internal static class LogSortFields
{
    public const string Timestamp = "timestamp";

    public static bool IsSupported(string? field) =>
        string.IsNullOrWhiteSpace(field) ||
        field.Trim().Equals(Timestamp, StringComparison.OrdinalIgnoreCase);
}

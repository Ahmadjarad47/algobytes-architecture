using FluentValidation;

namespace algo.Application.Features.ErrorLogs.Queries.GetErrorLogs;

public sealed class GetErrorLogsQueryValidator : AbstractValidator<GetErrorLogsQuery>
{
    public GetErrorLogsQueryValidator()
    {
        RuleFor(x => x.Pagination.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.Pagination.PageSize).InclusiveBetween(1, 100);

        RuleFor(x => x.Filters.FromTimestamp)
            .LessThanOrEqualTo(x => x.Filters.ToTimestamp)
            .When(x => x.Filters.FromTimestamp.HasValue && x.Filters.ToTimestamp.HasValue);

        RuleFor(x => x.Sort.Field)
            .Must(ErrorLogSortFields.IsSupported)
            .WithMessage($"Sort field must be '{ErrorLogSortFields.Timestamp}'.");
    }
}

internal static class ErrorLogSortFields
{
    public const string Timestamp = "timestamp";

    public static bool IsSupported(string? field) =>
        string.IsNullOrWhiteSpace(field) ||
        field.Trim().Equals(Timestamp, StringComparison.OrdinalIgnoreCase);
}

using algo.Application.Common.Filtering;
using algo.Application.Features.Users.Validation;
using FluentValidation;

namespace algo.Application.Features.Users.Queries.GetUsers;

public sealed class GetUsersQueryValidator : AbstractValidator<GetUsersQuery>
{
    public GetUsersQueryValidator()
    {
        RuleFor(x => x.Pagination.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.Pagination.PageSize).InclusiveBetween(1, 100);

        When(x => x.Sort is not null && !string.IsNullOrWhiteSpace(x.Sort.Field), () =>
        {
            RuleFor(x => x.Sort!.Field!)
                .Must(f => UserSortFields.Allowed.Contains(f.Trim()))
                .WithMessage("Invalid sort field.");
        });

        RuleFor(x => x).Custom(ValidateDateRanges);
    }

    private static void ValidateDateRanges(GetUsersQuery query, ValidationContext<GetUsersQuery> context)
    {
        ValidateRange(query.Filters?.CreatedAt, context, "filters.createdAt");
        ValidateRange(query.Filters?.LastLoginAt, context, "filters.lastLoginAt");
    }

    private static void ValidateRange(DateRangeFilter? range, ValidationContext<GetUsersQuery> context, string prefix)
    {
        if (range?.From is { } from && range.To is { } to && from > to)
            context.AddFailure(prefix, "The date range From must be less than or equal to To.");
    }
}

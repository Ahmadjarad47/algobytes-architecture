using algo.Application.Abstractions;
using algo.Application.Common.CustomFields;
using algo.Application.Common.Filtering;
using algo.Application.Features.Users.Validation;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace algo.Application.Features.Users.Queries.GetUsers;

public sealed class GetUsersQueryValidator : AbstractValidator<GetUsersQuery>
{
    private const string CustomFieldsPrefix = "customFields.";

    public GetUsersQueryValidator(IApplicationDbContext db)
    {
        RuleFor(x => x.Pagination.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.Pagination.PageSize).InclusiveBetween(1, 100);

        When(x => x.Sort is not null && !string.IsNullOrWhiteSpace(x.Sort.Field), () =>
        {
            RuleFor(x => x.Sort!.Field!)
                .MustAsync((field, cancellationToken) => BeValidSortFieldAsync(db, field, cancellationToken))
                .WithMessage("Invalid sort field.");
        });

        RuleFor(x => x).Custom(ValidateDateRanges);
    }

    private static async Task<bool> BeValidSortFieldAsync(
        IApplicationDbContext db,
        string field,
        CancellationToken cancellationToken)
    {
        var trimmed = field.Trim();
        if (UserSortFields.Allowed.Contains(trimmed))
        {
            return true;
        }

        if (!trimmed.StartsWith(CustomFieldsPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var key = trimmed[CustomFieldsPrefix.Length..];
        return await db.CustomFieldDefinitions
            .AsNoTracking()
            .AnyAsync(
                definition =>
                    definition.Entity == CustomFieldEntities.Users &&
                    definition.Sortable &&
                    definition.Key.ToLower() == key.ToLower(),
                cancellationToken);
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

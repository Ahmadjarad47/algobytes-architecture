using System.Text.Json;
using FluentValidation;

namespace algo.Application.Features.Users.Queries.SearchUsers;

public sealed class SearchUsersQueryValidator : AbstractValidator<SearchUsersQuery>
{
    private static readonly HashSet<string> AllowedFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "email", "userName", "displayName", "phoneNumber", "isActive", "emailConfirmed",
        "phoneNumberConfirmed", "createdAt", "updatedAt", "lastLoginAt", "status", "role"
    };

    private static readonly HashSet<string> AllowedOperators = new(StringComparer.OrdinalIgnoreCase)
    {
        "eq", "ne", "in", "nin", "contains", "startsWith", "endsWith", "gt", "gte", "lt", "lte", "isNull", "isNotNull"
    };

    private static readonly HashSet<string> AllowedIncludes = new(StringComparer.OrdinalIgnoreCase)
    {
        "roles", "permissions"
    };

    public SearchUsersQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.Limit).InclusiveBetween(1, 100);

        RuleForEach(x => x.Filters).Custom(ValidateFilter);
        RuleForEach(x => x.Sort).Custom(ValidateSort);

        RuleForEach(x => x.Include).Must(i => AllowedIncludes.Contains(i.Trim()))
            .WithMessage("Invalid include value. Allowed: roles, permissions.");
    }

    private static void ValidateFilter(SearchUsersFilterDto filter, ValidationContext<SearchUsersQuery> context)
    {
        if (string.IsNullOrWhiteSpace(filter.Field) || !AllowedFields.Contains(filter.Field.Trim()))
            context.AddFailure("filters.field", $"Field '{filter.Field}' is not allowed.");

        if (string.IsNullOrWhiteSpace(filter.Operator) || !AllowedOperators.Contains(filter.Operator.Trim()))
            context.AddFailure("filters.operator", $"Operator '{filter.Operator}' is not allowed.");

        var op = filter.Operator?.Trim();
        if (string.Equals(op, "in", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(op, "nin", StringComparison.OrdinalIgnoreCase))
        {
            if (filter.Value.ValueKind != JsonValueKind.Array)
                context.AddFailure("filters.value", "Operator in/nin expects an array value.");
        }
    }

    private static void ValidateSort(SearchUsersSortDto sort, ValidationContext<SearchUsersQuery> context)
    {
        if (string.IsNullOrWhiteSpace(sort.Field) || !AllowedFields.Contains(sort.Field.Trim()))
            context.AddFailure("sort.field", $"Sort field '{sort.Field}' is not allowed.");

        if (!string.Equals(sort.Direction, "asc", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(sort.Direction, "desc", StringComparison.OrdinalIgnoreCase))
            context.AddFailure("sort.direction", "Sort direction must be asc or desc.");
    }
}

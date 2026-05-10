using FluentValidation;

namespace algo.Application.Features.Users.Queries.GetUserPermissionGraph;

public sealed class GetUserPermissionGraphQueryValidator : AbstractValidator<GetUserPermissionGraphQuery>
{
    public GetUserPermissionGraphQueryValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}

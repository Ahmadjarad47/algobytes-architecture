using FluentValidation;

namespace algo.Application.Features.Users.Commands.AssignRoles;

public sealed class AssignRolesCommandValidator : AbstractValidator<AssignRolesCommand>
{
    public AssignRolesCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.RoleNames).NotEmpty().WithMessage("At least one role must be specified.");
        RuleForEach(x => x.RoleNames).NotEmpty().MaximumLength(256);
    }
}

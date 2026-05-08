using FluentValidation;

namespace algo.Application.Features.Users.Commands.RemoveRoles;

public sealed class RemoveRolesCommandValidator : AbstractValidator<RemoveRolesCommand>
{
    public RemoveRolesCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.RoleNames).NotEmpty().WithMessage("At least one role must be specified.");
        RuleForEach(x => x.RoleNames).NotEmpty().MaximumLength(256);
    }
}

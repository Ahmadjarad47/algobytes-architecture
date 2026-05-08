using algo.Application.Features.Roles.Validation;
using FluentValidation;

namespace algo.Application.Features.Roles.Commands.CreateRole;

public sealed class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MinimumLength(RoleValidationConstants.MinNameLength)
            .MaximumLength(RoleValidationConstants.MaxNameLength)
            .Matches(RoleValidationConstants.AllowedNameCharactersPattern)
            .WithMessage(RoleValidationConstants.AllowedNameCharactersMessage);
    }
}

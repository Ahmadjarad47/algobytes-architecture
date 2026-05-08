using algo.Application.Features.Roles.Validation;
using FluentValidation;

namespace algo.Application.Features.Roles.Commands.UpdateRole;

public sealed class UpdateRoleCommandValidator : AbstractValidator<UpdateRoleCommand>
{
    public UpdateRoleCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MinimumLength(RoleValidationConstants.MinNameLength)
            .MaximumLength(RoleValidationConstants.MaxNameLength)
            .Matches(RoleValidationConstants.AllowedNameCharactersPattern)
            .WithMessage(RoleValidationConstants.AllowedNameCharactersMessage);
    }
}

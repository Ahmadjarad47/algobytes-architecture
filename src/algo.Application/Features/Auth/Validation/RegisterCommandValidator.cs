using algo.Application.Features.Auth.Commands.Register;
using FluentValidation;

namespace algo.Application.Features.Auth.Validation;

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.ConfirmPassword)
            .NotEmpty()
            .Equal(x => x.Password)
            .WithMessage("Password and confirmation password must match.");
        RuleFor(x => x.DisplayName).NotEmpty();
    }
}

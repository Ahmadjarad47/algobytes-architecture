using algo.Application.Features.Auth.Commands.ForgotPassword;
using FluentValidation;

namespace algo.Application.Features.Auth.Validation;

public sealed class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}

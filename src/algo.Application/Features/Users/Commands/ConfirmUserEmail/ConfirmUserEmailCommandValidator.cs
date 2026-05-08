using FluentValidation;

namespace algo.Application.Features.Users.Commands.ConfirmUserEmail;

public sealed class ConfirmUserEmailCommandValidator : AbstractValidator<ConfirmUserEmailCommand>
{
    public ConfirmUserEmailCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}

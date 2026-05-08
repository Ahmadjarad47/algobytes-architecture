using FluentValidation;

namespace algo.Application.Features.Users.Commands.UnlockUser;

public sealed class UnlockUserCommandValidator : AbstractValidator<UnlockUserCommand>
{
    public UnlockUserCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}

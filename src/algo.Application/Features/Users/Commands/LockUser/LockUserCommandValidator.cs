using FluentValidation;

namespace algo.Application.Features.Users.Commands.LockUser;

public sealed class LockUserCommandValidator : AbstractValidator<LockUserCommand>
{
    public LockUserCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.LockoutEnd)
            .Must(end => end > DateTimeOffset.UtcNow)
            .WithMessage("Lockout end must be in the future.");
    }
}

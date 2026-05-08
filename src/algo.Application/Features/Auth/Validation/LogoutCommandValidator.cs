using algo.Application.Features.Auth.Commands.Logout;
using FluentValidation;

namespace algo.Application.Features.Auth.Validation;

public sealed class LogoutCommandValidator : AbstractValidator<LogoutCommand>
{
    public LogoutCommandValidator()
    {
        When(x => x.RefreshToken is not null, () =>
        {
            RuleFor(x => x.RefreshToken!).NotEmpty();
        });
    }
}

using algo.Application.Features.Auth.Commands.RefreshToken;
using FluentValidation;

namespace algo.Application.Features.Auth.Validation;

public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}

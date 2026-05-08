using algo.Application.Features.Auth.Commands.ResendOtp;
using FluentValidation;

namespace algo.Application.Features.Auth.Validation;

public sealed class ResendOtpCommandValidator : AbstractValidator<ResendOtpCommand>
{
    public ResendOtpCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}

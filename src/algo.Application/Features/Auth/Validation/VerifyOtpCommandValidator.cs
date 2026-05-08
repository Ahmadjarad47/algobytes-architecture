using algo.Application.Features.Auth.Commands.VerifyOtp;
using FluentValidation;

namespace algo.Application.Features.Auth.Validation;

public sealed class VerifyOtpCommandValidator : AbstractValidator<VerifyOtpCommand>
{
    public VerifyOtpCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Code).NotEmpty().Length(4, 12);
    }
}

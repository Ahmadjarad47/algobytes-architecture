using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace algo.Application.Features.Users.Commands.ChangeUserPassword;

public sealed class ChangeUserPasswordCommandValidator : AbstractValidator<ChangeUserPasswordCommand>
{
    public ChangeUserPasswordCommandValidator(IOptions<IdentityOptions> identityOptions)
    {
        var po = identityOptions.Value.Password;

        RuleFor(x => x.UserId).NotEmpty();

        RuleFor(x => x.NewPassword).NotEmpty();
        RuleFor(x => x.NewPassword).MinimumLength(po.RequiredLength);
        if (po.RequireDigit)
            RuleFor(x => x.NewPassword).Matches(@"\d").WithMessage("Password must contain at least one digit.");
        if (po.RequireLowercase)
            RuleFor(x => x.NewPassword).Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.");
        if (po.RequireUppercase)
            RuleFor(x => x.NewPassword).Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.");
        if (po.RequireNonAlphanumeric)
            RuleFor(x => x.NewPassword).Matches(@"[^a-zA-Z0-9]").WithMessage("Password must contain at least one non-alphanumeric character.");

        RuleFor(x => x.ConfirmPassword).NotEmpty().Equal(x => x.NewPassword).WithMessage("Passwords must match.");
    }
}

using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace algo.Application.Features.Users.Commands.CreateUser;

public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator(IOptions<IdentityOptions> identityOptions)
    {
        var po = identityOptions.Value.Password;

        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.UserName).NotEmpty().MaximumLength(256);
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(256);
        RuleFor(x => x.PhoneNumber).MaximumLength(50);

        RuleFor(x => x.Password).NotEmpty();
        RuleFor(x => x.Password).MinimumLength(po.RequiredLength);
        if (po.RequireDigit)
            RuleFor(x => x.Password).Matches(@"\d").WithMessage("Password must contain at least one digit.");
        if (po.RequireLowercase)
            RuleFor(x => x.Password).Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.");
        if (po.RequireUppercase)
            RuleFor(x => x.Password).Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.");
        if (po.RequireNonAlphanumeric)
            RuleFor(x => x.Password).Matches(@"[^a-zA-Z0-9]").WithMessage("Password must contain at least one non-alphanumeric character.");

        RuleFor(x => x.ConfirmPassword).NotEmpty().Equal(x => x.Password).WithMessage("Passwords must match.");

        When(x => x.Roles is not null, () =>
        {
            RuleForEach(x => x.Roles!).NotEmpty().MaximumLength(256);
        });
    }
}

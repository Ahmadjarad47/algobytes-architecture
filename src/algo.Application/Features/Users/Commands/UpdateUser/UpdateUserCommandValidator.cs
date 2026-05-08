using FluentValidation;

namespace algo.Application.Features.Users.Commands.UpdateUser;

public sealed class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();

        RuleFor(x => x.DisplayName).MaximumLength(256).When(x => x.DisplayName is not null);
        RuleFor(x => x.PhoneNumber).MaximumLength(50).When(x => x.PhoneNumber is not null);
        RuleFor(x => x.UserName).MaximumLength(256).When(x => x.UserName is not null);

        RuleFor(x => x)
            .Must(x =>
                x.DisplayName is not null ||
                x.PhoneNumber is not null ||
                x.UserName is not null ||
                x.IsActive is not null ||
                x.EmailConfirmed is not null)
            .WithMessage("At least one field must be provided to update.");
    }
}

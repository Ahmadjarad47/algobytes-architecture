using algo.Application.Abstractions;
using algo.Application.Common.AccessPolicy;
using algo.Application.Features.Users;
using algo.Domain.Identity.Entities;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace algo.Application.Features.Users.Commands.ActivateUser;

public sealed class ActivateUserCommandHandler(
    UserManager<ApplicationUser> userManager,
    IApplicationDbContext db,
    IAccessPolicyEvaluator accessPolicyEvaluator)
    : IRequestHandler<ActivateUserCommand, Unit>
{
    public async Task<Unit> Handle(ActivateUserCommand request, CancellationToken cancellationToken)
    {
        await accessPolicyEvaluator.EnsureResourceActionAllowedAsync(
            db,
            AccessPolicyResources.Users,
            AccessPolicyActions.Update,
            cancellationToken);

        var scoped = await accessPolicyEvaluator.ApplyAsync(
            db.Users.AsNoTracking().Where(u => u.Id == request.UserId),
            AccessPolicyResources.Users,
            AccessPolicyActions.Update,
            cancellationToken);

        if (!await scoped.AnyAsync(cancellationToken))
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(ActivateUserCommand.UserId), "User was not found."),
            });
        }

        var user = await userManager.FindByIdAsync(request.UserId)
            ?? throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(ActivateUserCommand.UserId), "User was not found."),
            });

        user.IsActive = true;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        var result = await userManager.UpdateAsync(user);
        result.ThrowIfFailed();
        return Unit.Value;
    }
}

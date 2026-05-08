using algo.Application.Abstractions;
using algo.Application.Common.AccessPolicy;
using algo.Application.Features.Users;
using algo.Domain.Identity.Entities;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace algo.Application.Features.Users.Commands.LockUser;

public sealed class LockUserCommandHandler(
    UserManager<ApplicationUser> userManager,
    IApplicationDbContext db,
    IAccessPolicyEvaluator accessPolicyEvaluator)
    : IRequestHandler<LockUserCommand, Unit>
{
    public async Task<Unit> Handle(LockUserCommand request, CancellationToken cancellationToken)
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
                new ValidationFailure(nameof(LockUserCommand.UserId), "User was not found."),
            });
        }

        var user = await userManager.FindByIdAsync(request.UserId)
            ?? throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(LockUserCommand.UserId), "User was not found."),
            });

        user.LockoutEnabled = true;
        user.LockoutEnd = request.LockoutEnd;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        var result = await userManager.UpdateAsync(user);
        result.ThrowIfFailed();
        return Unit.Value;
    }
}

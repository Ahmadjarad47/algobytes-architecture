using algo.Application.Abstractions;
using algo.Application.Common.AccessPolicy;
using algo.Application.Features.Users;
using algo.Domain.Identity.Entities;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace algo.Application.Features.Users.Commands.DeactivateUser;

public sealed class DeactivateUserCommandHandler(
    UserManager<ApplicationUser> userManager,
    IApplicationDbContext db,
    IAccessPolicyAuthorizationChecker authorizationChecker,
    IAccessPolicyQueryFilter queryFilter)
    : IRequestHandler<DeactivateUserCommand, Unit>
{
    public async Task<Unit> Handle(DeactivateUserCommand request, CancellationToken cancellationToken)
    {
        await authorizationChecker.EnsureResourceActionAllowedAsync(
            db,
            AccessPolicyResources.Users,
            AccessPolicyActions.Update,
            cancellationToken);

        var scoped = await queryFilter.ApplyAsync(
            db.Users.AsNoTracking().Where(u => u.Id == request.UserId),
            AccessPolicyResources.Users,
            AccessPolicyActions.Update,
            cancellationToken);

        if (!await scoped.AnyAsync(cancellationToken))
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(DeactivateUserCommand.UserId), "User was not found."),
            });
        }

        var user = await userManager.FindByIdAsync(request.UserId)
            ?? throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(DeactivateUserCommand.UserId), "User was not found."),
            });

        user.IsActive = false;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        var result = await userManager.UpdateAsync(user);
        result.ThrowIfFailed();
        return Unit.Value;
    }
}

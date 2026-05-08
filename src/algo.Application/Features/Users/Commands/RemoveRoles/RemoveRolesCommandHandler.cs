using algo.Application.Abstractions;
using algo.Application.Common.AccessPolicy;
using algo.Application.Features.Users;
using algo.Domain.Identity.Entities;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace algo.Application.Features.Users.Commands.RemoveRoles;

public sealed class RemoveRolesCommandHandler(
    UserManager<ApplicationUser> userManager,
    IApplicationDbContext db,
    IAccessPolicyEvaluator accessPolicyEvaluator)
    : IRequestHandler<RemoveRolesCommand, Unit>
{
    public async Task<Unit> Handle(RemoveRolesCommand request, CancellationToken cancellationToken)
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
                new ValidationFailure(nameof(RemoveRolesCommand.UserId), "User was not found."),
            });
        }

        var user = await userManager.FindByIdAsync(request.UserId)
            ?? throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(RemoveRolesCommand.UserId), "User was not found."),
            });

        foreach (var roleName in request.RoleNames.Select(r => r.Trim()).DistinctBy(r => r, StringComparer.OrdinalIgnoreCase))
        {
            if (!await userManager.IsInRoleAsync(user, roleName))
                continue;

            var remove = await userManager.RemoveFromRoleAsync(user, roleName);
            remove.ThrowIfFailed(nameof(RemoveRolesCommand.RoleNames));
        }

        user.UpdatedAt = DateTimeOffset.UtcNow;
        var update = await userManager.UpdateAsync(user);
        update.ThrowIfFailed();
        return Unit.Value;
    }
}

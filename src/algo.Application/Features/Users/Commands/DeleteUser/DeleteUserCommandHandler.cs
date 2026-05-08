using algo.Application.Abstractions;
using algo.Application.Common.AccessPolicy;
using algo.Application.Features.Users;
using algo.Domain.Identity.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace algo.Application.Features.Users.Commands.DeleteUser;

public sealed class DeleteUserCommandHandler(
    UserManager<ApplicationUser> userManager,
    IAccessPolicyEvaluator accessPolicyEvaluator,
    IApplicationDbContext db)
    : IRequestHandler<DeleteUserCommand, bool>
{
    public async Task<bool> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        await accessPolicyEvaluator.EnsureResourceActionAllowedAsync(
            db,
            AccessPolicyResources.Users,
            AccessPolicyActions.Delete,
            cancellationToken);

        var scoped = await accessPolicyEvaluator.ApplyAsync(
            db.Users.Where(u => u.Id == request.UserId),
            AccessPolicyResources.Users,
            AccessPolicyActions.Delete,
            cancellationToken);

        if (!await scoped.AnyAsync(cancellationToken))
            return false;

        var user = await userManager.FindByIdAsync(request.UserId);
        if (user is null)
            return false;

        var result = await userManager.DeleteAsync(user);
        result.ThrowIfFailed();
        return true;
    }
}

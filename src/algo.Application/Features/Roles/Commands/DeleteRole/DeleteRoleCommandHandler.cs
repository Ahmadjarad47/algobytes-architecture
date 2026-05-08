using algo.Application.Abstractions;
using algo.Application.Common.AccessPolicy;
using algo.Application.Features.Users;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace algo.Application.Features.Roles.Commands.DeleteRole;

public sealed class DeleteRoleCommandHandler(
    RoleManager<IdentityRole> roleManager,
    IAccessPolicyEvaluator accessPolicyEvaluator,
    IApplicationDbContext db)
    : IRequestHandler<DeleteRoleCommand, bool>
{
    public async Task<bool> Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
    {
        await accessPolicyEvaluator.EnsureResourceActionAllowedAsync(
            db,
            AccessPolicyResources.Roles,
            AccessPolicyActions.Delete,
            cancellationToken);

        if (!await db.Roles.AsNoTracking().AnyAsync(r => r.Id == request.Id, cancellationToken))
            return false;

        var role = await roleManager.FindByIdAsync(request.Id);
        if (role is null)
            return false;

        var result = await roleManager.DeleteAsync(role);
        result.ThrowIfFailed();
        return true;
    }
}

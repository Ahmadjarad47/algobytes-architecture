using algo.Application.Abstractions;
using algo.Application.Common.AccessPolicy;
using algo.Domain.Identity.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace algo.Application.Features.Users.Commands.SetUserTotpPolicy;

public sealed class SetUserTotpPolicyCommandHandler(
    IApplicationDbContext db,
    UserManager<ApplicationUser> userManager,
    IAccessPolicyAuthorizationChecker authorizationChecker,
    IAccessPolicyQueryFilter queryFilter) : IRequestHandler<SetUserTotpPolicyCommand, bool>
{
    public async Task<bool> Handle(SetUserTotpPolicyCommand request, CancellationToken cancellationToken)
    {
        await authorizationChecker.EnsureResourceActionAllowedAsync(
            db,
            AccessPolicyResources.Users,
            AccessPolicyActions.Update,
            cancellationToken);

        IQueryable<ApplicationUser> scoped = db.Users.AsNoTracking().Where(u => u.Id == request.UserId);
        scoped = await queryFilter.ApplyAsync(
            scoped,
            AccessPolicyResources.Users,
            AccessPolicyActions.Update,
            cancellationToken);

        if (!await scoped.AnyAsync(cancellationToken))
        {
            return false;
        }

        var user = await userManager.FindByIdAsync(request.UserId);
        if (user is null)
        {
            return false;
        }

        user.TotpRequiredByAdmin = request.IsRequired;

        if (!request.IsRequired)
        {
            user.TwoFactorEnabled = false;
        }

        user.UpdatedAt = DateTimeOffset.UtcNow;
        var result = await userManager.UpdateAsync(user);
        return result.Succeeded;
    }
}

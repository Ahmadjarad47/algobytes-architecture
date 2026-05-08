using algo.Application.Abstractions;
using algo.Application.Common.AccessPolicy;
using algo.Application.Features.Users.Dtos;
using algo.Domain.Identity.Entities;
using Mapster;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace algo.Application.Features.Users.Queries.GetUserById;

public sealed class GetUserByIdQueryHandler(
    IApplicationDbContext db,
    UserManager<ApplicationUser> userManager,
    IAccessPolicyEvaluator accessPolicyEvaluator) : IRequestHandler<GetUserByIdQuery, UserDetailsDto?>
{
    public async Task<UserDetailsDto?> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        await accessPolicyEvaluator.EnsureResourceActionAllowedAsync(
            db,
            AccessPolicyResources.Users,
            AccessPolicyActions.Read,
            cancellationToken);

        IQueryable<ApplicationUser> scoped = db.Users.AsNoTracking().Where(u => u.Id == request.UserId);
        scoped = await accessPolicyEvaluator.ApplyAsync(
            scoped,
            AccessPolicyResources.Users,
            AccessPolicyActions.Read,
            cancellationToken);

        if (!await scoped.AnyAsync(cancellationToken))
            return null;

        var user = await userManager.FindByIdAsync(request.UserId);
        if (user is null)
            return null;

        var roles = (await userManager.GetRolesAsync(user)).OrderBy(r => r).ToArray();
        var dto = user.Adapt<UserDetailsDto>();
        return dto with { Roles = roles };
    }
}

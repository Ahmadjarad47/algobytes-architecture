using algo.Application.Abstractions;
using algo.Application.Common.AccessPolicy;
using algo.Application.Features.Roles.Dtos;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace algo.Application.Features.Roles.Queries.GetRoles;

public sealed class GetRolesQueryHandler(
    IApplicationDbContext db,
    RoleManager<IdentityRole> roleManager,
    IAccessPolicyEvaluator accessPolicyEvaluator)
    : IRequestHandler<GetRolesQuery, IReadOnlyList<RoleDto>>
{
    public async Task<IReadOnlyList<RoleDto>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
    {
        await accessPolicyEvaluator.EnsureResourceActionAllowedAsync(
            db,
            AccessPolicyResources.Roles,
            AccessPolicyActions.Read,
            cancellationToken);

        var query = roleManager.Roles
            .AsNoTracking()
            .AsQueryable();

        return await query
            .OrderBy(r => r.Name)
            .Select(r => new RoleDto(r.Id, r.Name!, r.NormalizedName))
            .ToListAsync(cancellationToken);
    }
}

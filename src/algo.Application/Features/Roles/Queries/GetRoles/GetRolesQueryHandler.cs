using algo.Application.Abstractions;
using algo.Application.Common.AccessPolicy;
using algo.Application.Common.CustomFields;
using algo.Application.Features.Roles.Dtos;
using algo.Domain.Identity.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace algo.Application.Features.Roles.Queries.GetRoles;

public sealed class GetRolesQueryHandler(
    IApplicationDbContext db,
    RoleManager<ApplicationRole> roleManager,
    IAccessPolicyAuthorizationChecker authorizationChecker)
    : IRequestHandler<GetRolesQuery, IReadOnlyList<RoleDto>>
{
    public async Task<IReadOnlyList<RoleDto>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
    {
        await authorizationChecker.EnsureResourceActionAllowedAsync(
            db,
            AccessPolicyResources.Roles,
            AccessPolicyActions.Read,
            cancellationToken);

        IQueryable<ApplicationRole> query = request.IncludeTrashed || request.OnlyTrashed
            ? db.Roles.IgnoreQueryFilters().AsNoTracking()
            : roleManager.Roles.AsNoTracking().AsQueryable();

        if (request.OnlyTrashed)
        {
            query = query.Where(role => role.TrashedAt != null && role.DeletedAt == null);
        }
        else if (!request.IncludeTrashed)
        {
            query = query.Where(role => role.TrashedAt == null && role.DeletedAt == null);
        }

        var roles = await query
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);

        return roles
            .Select(role => new RoleDto(
                role.Id,
                role.Name!,
                role.NormalizedName,
                role.TrashedAt,
                role.TrashExpiresAt,
                role.DeletedAt,
                JsonDocumentHelpers.CloneToElement(role.CustomFields)))
            .ToList();
    }
}

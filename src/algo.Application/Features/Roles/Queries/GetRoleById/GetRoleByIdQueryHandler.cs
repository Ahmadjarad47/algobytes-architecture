using algo.Application.Abstractions;
using algo.Application.Common.AccessPolicy;
using algo.Application.Common.CustomFields;
using algo.Application.Features.Roles.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace algo.Application.Features.Roles.Queries.GetRoleById;

public sealed class GetRoleByIdQueryHandler(
    IAccessPolicyAuthorizationChecker authorizationChecker,
    IApplicationDbContext db) : IRequestHandler<GetRoleByIdQuery, RoleDetailsDto?>
{
    public async Task<RoleDetailsDto?> Handle(GetRoleByIdQuery request, CancellationToken cancellationToken)
    {
        await authorizationChecker.EnsureResourceActionAllowedAsync(
            db,
            AccessPolicyResources.Roles,
            AccessPolicyActions.Read,
            cancellationToken);

        var role = await db.Roles
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.Id && r.DeletedAt == null, cancellationToken);
        if (role is null)
            return null;

        var userCount = await db.UserRoles
            .AsNoTracking()
            .CountAsync(ur => ur.RoleId == role.Id, cancellationToken);

        return new RoleDetailsDto(
            role.Id,
            role.Name!,
            role.NormalizedName,
            userCount,
            role.TrashedAt,
            role.TrashExpiresAt,
            role.DeletedAt,
            JsonDocumentHelpers.CloneToElement(role.CustomFields));
    }
}

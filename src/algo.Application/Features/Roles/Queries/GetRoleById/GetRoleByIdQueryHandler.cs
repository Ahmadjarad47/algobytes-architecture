using algo.Application.Abstractions;
using algo.Application.Common.AccessPolicy;
using algo.Application.Features.Roles.Dtos;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace algo.Application.Features.Roles.Queries.GetRoleById;

public sealed class GetRoleByIdQueryHandler(
    IAccessPolicyEvaluator accessPolicyEvaluator,
    IApplicationDbContext db) : IRequestHandler<GetRoleByIdQuery, RoleDetailsDto?>
{
    public async Task<RoleDetailsDto?> Handle(GetRoleByIdQuery request, CancellationToken cancellationToken)
    {
        await accessPolicyEvaluator.EnsureResourceActionAllowedAsync(
            db,
            AccessPolicyResources.Roles,
            AccessPolicyActions.Read,
            cancellationToken);

        var role = await db.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);
        if (role is null)
            return null;

        var userCount = await db.UserRoles
            .AsNoTracking()
            .CountAsync(ur => ur.RoleId == role.Id, cancellationToken);

        return new RoleDetailsDto(role.Id, role.Name!, role.NormalizedName, userCount);
    }
}

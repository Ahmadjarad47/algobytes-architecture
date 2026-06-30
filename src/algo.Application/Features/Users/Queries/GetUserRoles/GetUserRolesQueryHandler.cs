using algo.Application.Abstractions;
using algo.Application.Common.AccessPolicy;
using algo.Application.Features.Users.Dtos;
using algo.Domain.Identity.Entities;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace algo.Application.Features.Users.Queries.GetUserRoles;

public sealed class GetUserRolesQueryHandler(
    IApplicationDbContext db,
    IAccessPolicyAuthorizationChecker authorizationChecker,
    IAccessPolicyQueryFilter queryFilter) : IRequestHandler<GetUserRolesQuery, IReadOnlyList<UserRoleDto>>
{
    public async Task<IReadOnlyList<UserRoleDto>> Handle(GetUserRolesQuery request, CancellationToken cancellationToken)
    {
        await authorizationChecker.EnsureResourceActionAllowedAsync(
            db,
            AccessPolicyResources.Users,
            AccessPolicyActions.Read,
            cancellationToken);

        IQueryable<ApplicationUser> scoped = db.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(u => u.Id == request.UserId && u.DeletedAt == null);
        scoped = await queryFilter.ApplyAsync(
            scoped,
            AccessPolicyResources.Users,
            AccessPolicyActions.Read,
            cancellationToken);

        if (!await scoped.AnyAsync(cancellationToken))
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(GetUserRolesQuery.UserId), "User was not found."),
            });
        }

        var roles = await (
                from ur in db.UserRoles
                join r in db.Roles on ur.RoleId equals r.Id
                where ur.UserId == request.UserId
                orderby r.Name
                select new UserRoleDto(r.Id, r.Name!))
            .ToListAsync(cancellationToken);

        return roles;
    }
}

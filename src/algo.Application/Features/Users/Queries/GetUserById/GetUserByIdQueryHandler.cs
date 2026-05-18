using algo.Application.Abstractions;
using algo.Application.Common.AccessPolicy;
using algo.Application.Common.CustomFields;
using algo.Application.Features.Users.Dtos;
using algo.Domain.Identity.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace algo.Application.Features.Users.Queries.GetUserById;

public sealed class GetUserByIdQueryHandler(
    IApplicationDbContext db,
    IAccessPolicyEvaluator accessPolicyEvaluator) : IRequestHandler<GetUserByIdQuery, UserDetailsDto?>
{
    public async Task<UserDetailsDto?> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        await accessPolicyEvaluator.EnsureResourceActionAllowedAsync(
            db,
            AccessPolicyResources.Users,
            AccessPolicyActions.Read,
            cancellationToken);

        IQueryable<ApplicationUser> scoped = db.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(u => u.Id == request.UserId && u.DeletedAt == null);
        scoped = await accessPolicyEvaluator.ApplyAsync(
            scoped,
            AccessPolicyResources.Users,
            AccessPolicyActions.Read,
            cancellationToken);

        if (!await scoped.AnyAsync(cancellationToken))
            return null;

        var user = await db.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.UserId && u.DeletedAt == null, cancellationToken);
        if (user is null)
            return null;

        var roles = await (
                from ur in db.UserRoles
                join r in db.Roles on ur.RoleId equals r.Id
                where ur.UserId == user.Id
                orderby r.Name
                select r.Name!)
            .ToArrayAsync(cancellationToken);

        return new UserDetailsDto(
            user.Id,
            user.Email,
            user.UserName,
            user.DisplayName,
            user.PhoneNumber,
            user.EmailConfirmed,
            user.PhoneNumberConfirmed,
            user.IsActive,
            user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow,
            user.LockoutEnd,
            user.CreatedAt,
            user.UpdatedAt,
            user.LastLoginAt,
            user.TrashedAt,
            user.TrashExpiresAt,
            user.DeletedAt,
            JsonDocumentHelpers.CloneToElement(user.CustomFields),
            user.TwoFactorEnabled,
            user.TotpRequiredByAdmin,
            roles);
    }
}

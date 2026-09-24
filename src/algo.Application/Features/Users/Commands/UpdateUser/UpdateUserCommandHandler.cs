using algo.Application.Abstractions;
using algo.Application.Common.AccessPolicy;
using algo.Application.Common.CustomFields;
using algo.Application.Features.Users;
using algo.Application.Features.Users.Dtos;
using algo.Domain.Identity.Entities;
using Mapster;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace algo.Application.Features.Users.Commands.UpdateUser;

public sealed class UpdateUserCommandHandler(
    UserManager<ApplicationUser> userManager,
    IAccessPolicyAuthorizationChecker authorizationChecker,
    IAccessPolicyQueryFilter queryFilter,
    IApplicationDbContext db,
    CustomFieldValueValidator customFieldValueValidator)
    : IRequestHandler<UpdateUserCommand, UserDetailsDto?>
{
    public async Task<UserDetailsDto?> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        await authorizationChecker.EnsureResourceActionAllowedAsync(
            db,
            AccessPolicyResources.Users,
            AccessPolicyActions.Update,
            cancellationToken);

        var scoped = await queryFilter.ApplyAsync(
            db.Users.Where(u => u.Id == request.UserId),
            AccessPolicyResources.Users,
            AccessPolicyActions.Update,
            cancellationToken);

        if (!await scoped.AnyAsync(cancellationToken))
            return null;

        var user = await userManager.FindByIdAsync(request.UserId);
        if (user is null)
            return null;

        if (request.DisplayName is not null)
            user.DisplayName = request.DisplayName.Trim();

        if (request.PhoneNumber is not null)
            user.PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim();

        if (request.UserName is not null)
            user.UserName = request.UserName.Trim();

        if (request.IsActive is { } active)
            user.IsActive = active;

        if (request.EmailConfirmed is { } emailConfirmed)
            user.EmailConfirmed = emailConfirmed;

        user.CustomFields = await customFieldValueValidator.ValidateAndNormalizeAsync(
            CustomFieldEntities.Users,
            request.CustomFields,
            cancellationToken);

        user.UpdatedAt = DateTimeOffset.UtcNow;

        var update = await userManager.UpdateAsync(user);
        update.ThrowIfFailed();

        var roles = (await userManager.GetRolesAsync(user)).OrderBy(r => r).ToArray();
        var dto = user.Adapt<UserDetailsDto>();
        return dto with { Roles = roles };
    }
}

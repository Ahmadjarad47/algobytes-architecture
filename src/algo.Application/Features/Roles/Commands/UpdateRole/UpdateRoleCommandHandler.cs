using algo.Application.Abstractions;
using algo.Application.Common.AccessPolicy;
using algo.Application.Features.Roles.Dtos;
using algo.Application.Features.Users;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace algo.Application.Features.Roles.Commands.UpdateRole;

public sealed class UpdateRoleCommandHandler(
    RoleManager<IdentityRole> roleManager,
    IApplicationDbContext db,
    IAccessPolicyEvaluator accessPolicyEvaluator) : IRequestHandler<UpdateRoleCommand, RoleDetailsDto?>
{
    public async Task<RoleDetailsDto?> Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
    {
        await accessPolicyEvaluator.EnsureResourceActionAllowedAsync(
            db,
            AccessPolicyResources.Roles,
            AccessPolicyActions.Update,
            cancellationToken);

        if (!await db.Roles.AsNoTracking().AnyAsync(r => r.Id == request.Id, cancellationToken))
            return null;

        var role = await roleManager.FindByIdAsync(request.Id);
        if (role is null)
            return null;

        var newName = request.Name.Trim();
        var isRename = !string.Equals(role.Name, newName, StringComparison.OrdinalIgnoreCase);

        if (isRename && await roleManager.RoleExistsAsync(newName))
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(UpdateRoleCommand.Name), $"Role '{newName}' already exists."),
            });
        }

        if (isRename)
        {
            var setName = await roleManager.SetRoleNameAsync(role, newName);
            setName.ThrowIfFailed(nameof(UpdateRoleCommand.Name));
        }

        var update = await roleManager.UpdateAsync(role);
        update.ThrowIfFailed(nameof(UpdateRoleCommand.Name));

        var userCount = await db.UserRoles.AsNoTracking().CountAsync(ur => ur.RoleId == role.Id, cancellationToken);
        return new RoleDetailsDto(role.Id, role.Name!, role.NormalizedName, userCount);
    }
}

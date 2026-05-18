using algo.Application.Abstractions;
using algo.Application.Common.AccessPolicy;
using algo.Application.Common.CustomFields;
using algo.Application.Features.Roles.Dtos;
using algo.Application.Features.Users;
using algo.Domain.Identity.Entities;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace algo.Application.Features.Roles.Commands.UpdateRole;

public sealed class UpdateRoleCommandHandler(
    RoleManager<ApplicationRole> roleManager,
    IApplicationDbContext db,
    IAccessPolicyEvaluator accessPolicyEvaluator,
    CustomFieldValueValidator customFieldValueValidator) : IRequestHandler<UpdateRoleCommand, RoleDetailsDto?>
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

        role.CustomFields = await customFieldValueValidator.ValidateAndNormalizeAsync(
            CustomFieldEntities.Roles,
            request.CustomFields,
            cancellationToken);

        var update = await roleManager.UpdateAsync(role);
        update.ThrowIfFailed(nameof(UpdateRoleCommand.Name));

        var userCount = await db.UserRoles.AsNoTracking().CountAsync(ur => ur.RoleId == role.Id, cancellationToken);
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

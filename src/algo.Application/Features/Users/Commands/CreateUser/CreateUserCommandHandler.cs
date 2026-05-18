using algo.Application.Abstractions;
using algo.Application.Common.AccessPolicy;
using algo.Application.Common.CustomFields;
using algo.Application.Features.Users;
using algo.Application.Features.Users.Dtos;
using algo.Domain.Identity.Entities;
using FluentValidation;
using FluentValidation.Results;
using Mapster;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace algo.Application.Features.Users.Commands.CreateUser;

public sealed class CreateUserCommandHandler(
    IApplicationDbContext db,
    IAccessPolicyEvaluator accessPolicyEvaluator,
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    CustomFieldValueValidator customFieldValueValidator) : IRequestHandler<CreateUserCommand, UserDetailsDto>
{
    public async Task<UserDetailsDto> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        await accessPolicyEvaluator.EnsureResourceActionAllowedAsync(
            db,
            AccessPolicyResources.Users,
            AccessPolicyActions.Create,
            cancellationToken);

        var rolesToAssign = request.Roles ?? [];

        foreach (var roleName in rolesToAssign)
        {
            if (string.IsNullOrWhiteSpace(roleName))
                continue;

            if (!await roleManager.RoleExistsAsync(roleName.Trim()))
            {
                throw new ValidationException(new[]
                {
                    new ValidationFailure(nameof(CreateUserCommand.Roles), $"Role '{roleName}' was not found."),
                });
            }
        }

        var utc = DateTimeOffset.UtcNow;
        var user = new ApplicationUser
        {
            Email = request.Email.Trim(),
            UserName = request.UserName.Trim(),
            DisplayName = request.DisplayName.Trim(),
            PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim(),
            EmailConfirmed = request.EmailConfirmed,
            IsActive = request.IsActive,
            CreatedAt = utc,
            UpdatedAt = utc,
            LockoutEnabled = true,
            CustomFields = await customFieldValueValidator.ValidateAndNormalizeAsync(
                CustomFieldEntities.Users,
                request.CustomFields,
                cancellationToken)
        };

        var createResult = await userManager.CreateAsync(user, request.Password);
        createResult.ThrowIfFailed(nameof(CreateUserCommand.Password));

        foreach (var roleName in rolesToAssign.Where(r => !string.IsNullOrWhiteSpace(r)).Select(r => r.Trim()))
        {
            var addRole = await userManager.AddToRoleAsync(user, roleName);
            addRole.ThrowIfFailed(nameof(CreateUserCommand.Roles));
        }

        var roles = (await userManager.GetRolesAsync(user)).OrderBy(r => r).ToArray();
        var dto = user.Adapt<UserDetailsDto>();
        return dto with { Roles = roles };
    }
}

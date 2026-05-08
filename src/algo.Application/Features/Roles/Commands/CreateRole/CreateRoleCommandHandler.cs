using algo.Application.Abstractions;
using algo.Application.Common.AccessPolicy;
using algo.Application.Features.Roles.Dtos;
using algo.Application.Features.Users;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace algo.Application.Features.Roles.Commands.CreateRole;

public sealed class CreateRoleCommandHandler(
    RoleManager<IdentityRole> roleManager,
    IAccessPolicyEvaluator accessPolicyEvaluator,
    IApplicationDbContext db)
    : IRequestHandler<CreateRoleCommand, RoleDetailsDto>
{
    public async Task<RoleDetailsDto> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        await accessPolicyEvaluator.EnsureResourceActionAllowedAsync(
            db,
            AccessPolicyResources.Roles,
            AccessPolicyActions.Create,
            cancellationToken);

        var name = request.Name.Trim();

        if (await roleManager.RoleExistsAsync(name))
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(CreateRoleCommand.Name), $"Role '{name}' already exists."),
            });
        }

        var role = new IdentityRole(name);

        var result = await roleManager.CreateAsync(role);
        result.ThrowIfFailed(nameof(CreateRoleCommand.Name));

        return new RoleDetailsDto(role.Id, role.Name!, role.NormalizedName, UserCount: 0);
    }
}

using algo.Application.Common.Identity;
using algo.Domain.Identity.Entities;
using algo.Domain.Identity.Policies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace algo.Persistence.Context;

public static class ApplicationDbContextSeeder
{
    public static async Task SeedAsync(
        IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var dbContext = services.GetRequiredService<ApplicationDbContext>();
        var logger = services.GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(ApplicationDbContextSeeder).FullName!);

        await SeedDefaultRolesAsync(roleManager, cancellationToken);
        await SeedDefaultAdminUserAsync(userManager, cancellationToken);
        await SeedDefaultAccessPoliciesAsync(dbContext, logger, cancellationToken);
    }

    private static async Task SeedDefaultRolesAsync(
        RoleManager<IdentityRole> roleManager,
        CancellationToken cancellationToken)
    {
        foreach (var roleName in DefaultRoles.All)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (await roleManager.RoleExistsAsync(roleName))
            {
                continue;
            }

            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }

    private static async Task SeedDefaultAdminUserAsync(
        UserManager<ApplicationUser> userManager,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var adminUser = await userManager.FindByEmailAsync(DefaultAdmin.Email);
        adminUser ??= await userManager.FindByNameAsync(DefaultAdmin.UserName);

        if (adminUser is null)
        {
            var utcNow = DateTimeOffset.UtcNow;
            adminUser = new ApplicationUser
            {
                UserName = DefaultAdmin.UserName,
                Email = DefaultAdmin.Email,
                DisplayName = DefaultAdmin.DisplayName,
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = utcNow,
                UpdatedAt = utcNow,
            };

            var createResult = await userManager.CreateAsync(adminUser, DefaultAdmin.Password);
            if (!createResult.Succeeded)
            {
                var errors = string.Join("; ", createResult.Errors.Select(e => $"{e.Code}: {e.Description}"));
                throw new InvalidOperationException($"Failed to create default admin user. {errors}");
            }
        }
        else
        {
            var hasUpdates = false;

            if (!adminUser.EmailConfirmed)
            {
                adminUser.EmailConfirmed = true;
                hasUpdates = true;
            }

            if (!adminUser.IsActive)
            {
                adminUser.IsActive = true;
                hasUpdates = true;
            }

            if (!string.Equals(adminUser.DisplayName, DefaultAdmin.DisplayName, StringComparison.Ordinal))
            {
                adminUser.DisplayName = DefaultAdmin.DisplayName;
                hasUpdates = true;
            }

            if (hasUpdates)
            {
                adminUser.UpdatedAt = DateTimeOffset.UtcNow;
                var updateResult = await userManager.UpdateAsync(adminUser);
                if (!updateResult.Succeeded)
                {
                    var errors = string.Join("; ", updateResult.Errors.Select(e => $"{e.Code}: {e.Description}"));
                    throw new InvalidOperationException($"Failed to update default admin user. {errors}");
                }
            }
        }

        if (!await userManager.IsInRoleAsync(adminUser, DefaultRoles.Admin))
        {
            var addToRoleResult = await userManager.AddToRoleAsync(adminUser, DefaultRoles.Admin);
            if (!addToRoleResult.Succeeded)
            {
                var errors = string.Join("; ", addToRoleResult.Errors.Select(e => $"{e.Code}: {e.Description}"));
                throw new InvalidOperationException($"Failed to assign default admin role. {errors}");
            }
        }
    }

    private static async Task SeedDefaultAccessPoliciesAsync(
        ApplicationDbContext dbContext,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var adminWildcardExists = await dbContext.AccessPolicies
            .AnyAsync(
                policy => policy.Resource == DefaultPermissions.WildcardResource &&
                          policy.Action == DefaultPermissions.WildcardAction &&
                          policy.SubjectType == AccessPolicySubjectType.Role &&
                          policy.SubjectKey == DefaultRoles.Admin &&
                          policy.Effect == AccessPolicyEffect.Allow &&
                          policy.IsEnabled &&
                          policy.DeletedAt == null,
                cancellationToken);

        if (adminWildcardExists)
        {
            logger.LogInformation("Admin wildcard access policy already exists");
            return;
        }

        dbContext.AccessPolicies.Add(new AccessPolicy
        {
            Resource = DefaultPermissions.WildcardResource,
            Action = DefaultPermissions.WildcardAction,
            Effect = AccessPolicyEffect.Allow,
            SubjectType = AccessPolicySubjectType.Role,
            SubjectKey = DefaultRoles.Admin,
            Priority = DefaultPermissions.AdminFullAccessPriority,
            IsEnabled = true,
            Description = DefaultPermissions.AdminFullAccessDescription,
            ConditionJson = null,
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Seeded Admin wildcard access policy");
    }
}

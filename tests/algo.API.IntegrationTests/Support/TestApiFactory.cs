using algo.Application.Common.AccessPolicy;
using algo.Application.Common.Identity;
using algo.Domain.Identity.Entities;
using algo.Domain.Identity.Policies;
using algo.Persistence.Context;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace algo.API.IntegrationTests.Support;

public sealed class TestApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private bool initialized;
    private readonly SemaphoreSlim initLock = new(1, 1);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            connection.Open();

            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<DbContextOptions>();
            services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();
            services.RemoveAll<ApplicationDbContext>();

            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connection));
            services.AddScoped<algo.Application.Abstractions.IApplicationDbContext>(sp =>
                sp.GetRequiredService<ApplicationDbContext>());

            using var scope = services.BuildServiceProvider().CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.EnsureCreated();
        });
    }

    public async Task EnsureLogsReaderAsync()
    {
        await EnsureInitializedAsync();

        using var scope = Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        const string roleName = "LogsReader";
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            var roleResult = await roleManager.CreateAsync(new ApplicationRole { Name = roleName });
            Assert.True(roleResult.Succeeded, string.Join("; ", roleResult.Errors.Select(e => e.Description)));
        }

        var user = await userManager.FindByEmailAsync("logs.reader@algo.bytes");
        if (user is null)
        {
            var utcNow = DateTimeOffset.UtcNow;
            user = new ApplicationUser
            {
                Email = "logs.reader@algo.bytes",
                UserName = "logs.reader",
                DisplayName = "Logs Reader",
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = utcNow,
                UpdatedAt = utcNow,
            };

            var createResult = await userManager.CreateAsync(user, "Reader@123456");
            Assert.True(createResult.Succeeded, string.Join("; ", createResult.Errors.Select(e => e.Description)));
        }

        if (!await userManager.IsInRoleAsync(user, roleName))
        {
            var addResult = await userManager.AddToRoleAsync(user, roleName);
            Assert.True(addResult.Succeeded, string.Join("; ", addResult.Errors.Select(e => e.Description)));
        }

        var hasLogsReadPolicy = await db.AccessPolicies.AnyAsync(
            p => p.SubjectType == AccessPolicySubjectType.Role
                && p.SubjectKey == roleName
                && p.Resource == AccessPolicyResources.Logs
                && p.Action == AccessPolicyActions.Read
                && p.Effect == AccessPolicyEffect.Allow
                && p.IsEnabled
                && p.DeletedAt == null);

        if (!hasLogsReadPolicy)
        {
            db.AccessPolicies.Add(new AccessPolicy
            {
                Resource = AccessPolicyResources.Logs,
                Action = AccessPolicyActions.Read,
                Effect = AccessPolicyEffect.Allow,
                SubjectType = AccessPolicySubjectType.Role,
                SubjectKey = roleName,
                Priority = 50,
                IsEnabled = true,
                Description = "Logs reader test policy",
            });
            await db.SaveChangesAsync();
        }
    }

    public Task EnsureInitializedForTestsAsync() => EnsureInitializedAsync();

    private async Task EnsureInitializedAsync()
    {
        if (initialized)
            return;

        await initLock.WaitAsync();
        try
        {
            if (initialized)
                return;

            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await db.Database.EnsureCreatedAsync();
            await ApplicationDbContextSeeder.SeedAsync(scope.ServiceProvider);
            initialized = true;
        }
        finally
        {
            initLock.Release();
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            connection.Dispose();
            initLock.Dispose();
        }
    }
}

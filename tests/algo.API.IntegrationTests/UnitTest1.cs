using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace algo.API.IntegrationTests;

public sealed class AuthorizationBehaviorTests : IClassFixture<AuthorizationBehaviorTests.TestApiFactory>
{
    private readonly TestApiFactory factory;

    public AuthorizationBehaviorTests(TestApiFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task UserWithOnlyLogsRead_IsForbiddenForOtherAdminResources()
    {
        await factory.EnsureLogsReaderAsync();
        using var client = factory.CreateClient();
        var token = await LoginAsync(client, "logs.reader@algo.bytes", "Reader@123456");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var logsResponse = await client.GetAsync("/api/logs");
        Assert.Equal(HttpStatusCode.OK, logsResponse.StatusCode);

        var listAccessPoliciesResponse = await client.GetAsync("/api/AccessPolicies");
        Assert.Equal(HttpStatusCode.Forbidden, listAccessPoliciesResponse.StatusCode);

        var createAccessPolicyResponse = await client.PostAsJsonAsync("/api/AccessPolicies", new
        {
            resource = AccessPolicyResources.Logs,
            action = AccessPolicyActions.Read,
            effect = "allow",
            subjectType = "role",
            subjectKey = "LogsReader",
            conditionJson = (string?)null,
            priority = 10,
            isEnabled = true,
            description = "test",
            validFrom = (DateTime?)null,
            validTo = (DateTime?)null,
        });
        Assert.Equal(HttpStatusCode.Forbidden, createAccessPolicyResponse.StatusCode);

        var rolesResponse = await client.GetAsync("/api/roles");
        Assert.Equal(HttpStatusCode.Forbidden, rolesResponse.StatusCode);

        var usersResponse = await client.GetAsync("/api/users");
        Assert.Equal(HttpStatusCode.Forbidden, usersResponse.StatusCode);
    }

    [Fact]
    public async Task AdminWithWildcard_IsAllowedForAdminAndBusinessResources()
    {
        await factory.EnsureInitializedForTestsAsync();
        using var client = factory.CreateClient();
        var token = await LoginAsync(client, DefaultAdmin.Email, DefaultAdmin.Password);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var accessPoliciesResponse = await client.GetAsync("/api/AccessPolicies");
        Assert.Equal(HttpStatusCode.OK, accessPoliciesResponse.StatusCode);

        var createAccessPolicyResponse = await client.PostAsJsonAsync("/api/AccessPolicies", new
        {
            resource = AccessPolicyResources.Logs,
            action = AccessPolicyActions.Read,
            effect = "allow",
            subjectType = "role",
            subjectKey = "Admin",
            conditionJson = (string?)null,
            priority = 42,
            isEnabled = true,
            description = "admin test policy",
            validFrom = (DateTime?)null,
            validTo = (DateTime?)null,
        });
        Assert.Equal(HttpStatusCode.OK, createAccessPolicyResponse.StatusCode);

        var usersResponse = await client.GetAsync("/api/users");
        Assert.Equal(HttpStatusCode.OK, usersResponse.StatusCode);

        var logsResponse = await client.GetAsync("/api/logs");
        Assert.Equal(HttpStatusCode.OK, logsResponse.StatusCode);
    }

    private static async Task<string> LoginAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<JsonObject>();
        var token = body?["tokens"]?["accessToken"]?.GetValue<string>();
        Assert.False(string.IsNullOrWhiteSpace(token));
        return token!;
    }

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
                services.RemoveAll<ApplicationDbContext>();

                services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connection));
                services.AddScoped<algo.Application.Abstractions.IApplicationDbContext>(sp =>
                    sp.GetRequiredService<ApplicationDbContext>());
            });
        }

        public async Task EnsureLogsReaderAsync()
        {
            await EnsureInitializedAsync();

            using var scope = Services.CreateScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            const string roleName = "LogsReader";
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var roleResult = await roleManager.CreateAsync(new IdentityRole(roleName));
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

        public Task EnsureInitializedForTestsAsync() => EnsureInitializedAsync();

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
}

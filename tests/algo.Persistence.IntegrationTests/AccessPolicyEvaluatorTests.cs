using algo.Application.Abstractions;
using algo.Application.Common.AccessPolicy;
using algo.Domain.Identity.Entities;
using algo.Domain.Identity.Policies;
using algo.Domain.Logging.Entities;
using algo.Persistence.Abac;
using algo.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;

namespace algo.Persistence.IntegrationTests;

public sealed class AccessPolicyEvaluatorTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new ApplicationDbContext(options);
    }

    private sealed class TestTokenResolver(string? userId, IReadOnlyList<string> roles) : IAccessPolicyTokenResolver
    {
        public string? CurrentUserId => userId;

        public IReadOnlyList<string> CurrentRoleNames => roles;

        public object? ResolveTokenValue(string token) => token switch
        {
            "@CurrentUserId" => CurrentUserId,
            "@CurrentRoleNames" => CurrentRoleNames,
            _ => null,
        };
    }

    private static AccessPolicyEvaluator CreateEvaluator(
        ApplicationDbContext db,
        IAccessPolicyTokenResolver tokens)
    {
        var metadata = new AccessPolicyMetadataProvider();
        var parser = new AccessPolicyConditionParser();
        var compiler = new AccessPolicyExpressionCompiler();
        var store = new AccessPolicyRuleStore(db);
        return new AccessPolicyEvaluator(store, parser, tokens, metadata, compiler, NullLogger<AccessPolicyEvaluator>.Instance);
    }

    [Fact]
    public async Task Admin_wildcard_allow_sees_all_users()
    {
        await using var db = CreateContext();
        db.Users.AddRange(
            CreateUser("1", "a@test", isActive: true),
            CreateUser("2", "b@test", isActive: false));
        db.AccessPolicies.Add(new AccessPolicy
        {
            Id = Guid.NewGuid(),
            Resource = "*",
            Action = "*",
            Effect = AccessPolicyEffect.Allow,
            SubjectType = AccessPolicySubjectType.Role,
            SubjectKey = "Admin",
            Priority = 1,
            IsEnabled = true,
        });
        await db.SaveChangesAsync();

        var evaluator = CreateEvaluator(db, new TestTokenResolver("1", ["Admin"]));
        var query = await evaluator.ApplyAsync(db.Users.AsQueryable(), AccessPolicyResources.Users, "read");

        Assert.Equal(2, await query.CountAsync());
    }

    [Fact]
    public async Task Admin_wildcard_allow_allows_accessPolicies_create()
    {
        await using var db = CreateContext();
        db.AccessPolicies.Add(new AccessPolicy
        {
            Id = Guid.NewGuid(),
            Resource = "*",
            Action = "*",
            Effect = AccessPolicyEffect.Allow,
            SubjectType = AccessPolicySubjectType.Role,
            SubjectKey = "Admin",
            Priority = 1,
            IsEnabled = true,
            DeletedAt = null,
            ConditionJson = null,
        });
        await db.SaveChangesAsync();

        var evaluator = CreateEvaluator(db, new TestTokenResolver("1", ["Admin"]));
        var scoped = await evaluator.ApplyAsync(
            db.AccessPolicies.AsQueryable(),
            AccessPolicyResources.AccessPolicies,
            AccessPolicyActions.Create);

        Assert.True(await scoped.AnyAsync());
    }

    [Fact]
    public async Task Admin_wildcard_allows_users_delete()
    {
        await using var db = CreateContext();
        db.Users.Add(CreateUser("1", "user@test", isActive: true));
        db.AccessPolicies.Add(new AccessPolicy
        {
            Id = Guid.NewGuid(),
            Resource = "*",
            Action = "*",
            Effect = AccessPolicyEffect.Allow,
            SubjectType = AccessPolicySubjectType.Role,
            SubjectKey = "Admin",
            Priority = 1,
            IsEnabled = true,
        });
        await db.SaveChangesAsync();

        var evaluator = CreateEvaluator(db, new TestTokenResolver("1", ["Admin"]));
        var allowed = await evaluator.IsAllowedAsync(AccessPolicyResources.Users, AccessPolicyActions.Delete);

        Assert.True(allowed);
    }

    [Fact]
    public async Task User_logs_read_does_not_allow_accessPolicies_create()
    {
        await using var db = CreateContext();
        db.AccessPolicies.Add(new AccessPolicy
        {
            Id = Guid.NewGuid(),
            Resource = AccessPolicyResources.Logs,
            Action = AccessPolicyActions.Read,
            Effect = AccessPolicyEffect.Allow,
            SubjectType = AccessPolicySubjectType.Role,
            SubjectKey = "User",
            Priority = 10,
            IsEnabled = true,
        });
        await db.SaveChangesAsync();

        var evaluator = CreateEvaluator(db, new TestTokenResolver("u1", ["User"]));
        var allowed = await evaluator.IsAllowedAsync(AccessPolicyResources.AccessPolicies, AccessPolicyActions.Create);

        Assert.False(allowed);
    }

    [Fact]
    public async Task User_logs_read_allows_logs_read()
    {
        await using var db = CreateContext();
        db.ApplicationLogs.Add(new ApplicationLog
        {
            Timestamp = DateTimeOffset.UtcNow,
            Level = "Information",
            Message = "ok",
        });
        db.AccessPolicies.Add(new AccessPolicy
        {
            Id = Guid.NewGuid(),
            Resource = AccessPolicyResources.Logs,
            Action = AccessPolicyActions.Read,
            Effect = AccessPolicyEffect.Allow,
            SubjectType = AccessPolicySubjectType.Role,
            SubjectKey = "User",
            Priority = 10,
            IsEnabled = true,
        });
        await db.SaveChangesAsync();

        var evaluator = CreateEvaluator(db, new TestTokenResolver("u1", ["User"]));
        var scoped = await evaluator.ApplyAsync(db.ApplicationLogs.AsQueryable(), AccessPolicyResources.Logs, AccessPolicyActions.Read);

        Assert.True(await scoped.AnyAsync());
    }

    [Fact]
    public async Task InactiveViewer_sees_only_inactive_users()
    {
        await using var db = CreateContext();
        db.Users.AddRange(
            CreateUser("1", "active@test", isActive: true),
            CreateUser("2", "inactive@test", isActive: false));
        db.AccessPolicies.AddRange(
            new AccessPolicy
            {
                Id = Guid.NewGuid(),
                Resource = "*",
                Action = "*",
                Effect = AccessPolicyEffect.Allow,
                SubjectType = AccessPolicySubjectType.Role,
                SubjectKey = "Admin",
                Priority = 1,
                IsEnabled = true,
            },
            new AccessPolicy
            {
                Id = Guid.NewGuid(),
                Resource = "users",
                Action = "read",
                Effect = AccessPolicyEffect.Allow,
                SubjectType = AccessPolicySubjectType.Role,
                SubjectKey = "InactiveViewer",
                Priority = 10,
                IsEnabled = true,
                ConditionJson = """{"field":"isActive","operator":"eq","value":false}""",
            });
        await db.SaveChangesAsync();

        var evaluator = CreateEvaluator(db, new TestTokenResolver("x", ["InactiveViewer"]));
        var query = await evaluator.ApplyAsync(db.Users.AsQueryable(), AccessPolicyResources.Users, "read");

        var emails = await query.Select(u => u.Email).OrderBy(e => e).ToListAsync();
        Assert.Single(emails);
        Assert.Equal("inactive@test", emails[0]);
    }

    [Fact]
    public async Task User_without_matching_policy_gets_empty_query()
    {
        await using var db = CreateContext();
        db.Users.Add(CreateUser("1", "a@test", isActive: true));
        await db.SaveChangesAsync();

        var evaluator = CreateEvaluator(db, new TestTokenResolver("1", ["NoPolicyRole"]));
        var query = await evaluator.ApplyAsync(db.Users.AsQueryable(), AccessPolicyResources.Users, "read");

        Assert.Equal(0, await query.CountAsync());
    }

    [Fact]
    public void Validate_condition_rejects_unknown_field()
    {
        var parser = new AccessPolicyConditionParser();
        var metadata = new AccessPolicyMetadataProvider();
        var ast = parser.Parse("""{"field":"notAField","operator":"eq","value":true}""");
        var ex = Assert.Throws<AccessPolicyConditionValidationException>(() =>
            parser.Validate(AccessPolicyResources.Users, ast, metadata));
        Assert.Contains("Unknown field", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_condition_rejects_unknown_resource()
    {
        var parser = new AccessPolicyConditionParser();
        var metadata = new AccessPolicyMetadataProvider();
        var ast = parser.Parse("""{"field":"isActive","operator":"eq","value":false}""");
        var ex = Assert.Throws<AccessPolicyConditionValidationException>(() =>
            parser.Validate("unknown_resource", ast, metadata));
        Assert.Contains("Unknown resource", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static ApplicationUser CreateUser(string id, string email, bool isActive) =>
        new()
        {
            Id = id,
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            DisplayName = email,
            IsActive = isActive,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
}

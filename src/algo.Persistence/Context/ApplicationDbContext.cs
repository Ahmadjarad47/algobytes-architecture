using algo.Application.Abstractions;
using algo.Domain.CustomFields;
using algo.Domain.Identity.Entities;
using algo.Domain.Logging.Entities;
using System.Text.Json;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using AccessPolicyEntity = algo.Domain.Identity.Policies.AccessPolicy;

namespace algo.Persistence.Context;

public sealed class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<AccessPolicyEntity> AccessPolicies => Set<AccessPolicyEntity>();

    public DbSet<CustomFieldDefinition> CustomFieldDefinitions => Set<CustomFieldDefinition>();

    public new DbSet<ApplicationRole> Roles => Set<ApplicationRole>();

    public DbSet<OtpToken> OtpTokens => Set<OtpToken>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<ApplicationLog> ApplicationLogs => Set<ApplicationLog>();

    public DbSet<ErrorLog> ErrorLogs => Set<ErrorLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        if (string.Equals(Database.ProviderName, "Microsoft.EntityFrameworkCore.Sqlite", StringComparison.Ordinal))
        {
            ApplySqliteJsonDocumentConversions(modelBuilder);
        }
    }

    private static void ApplySqliteJsonDocumentConversions(ModelBuilder modelBuilder)
    {
        var jsonDocumentConverter = new ValueConverter<JsonDocument?, string?>(
            document => document == null ? null : document.RootElement.GetRawText(),
            json => string.IsNullOrWhiteSpace(json) ? null : JsonDocument.Parse(json));

        modelBuilder.Entity<ApplicationUser>()
            .Property(user => user.CustomFields)
            .HasConversion(jsonDocumentConverter);

        modelBuilder.Entity<ApplicationRole>()
            .Property(role => role.CustomFields)
            .HasConversion(jsonDocumentConverter);

        modelBuilder.Entity<AccessPolicyEntity>()
            .Property(policy => policy.CustomFields)
            .HasConversion(jsonDocumentConverter);

        modelBuilder.Entity<CustomFieldDefinition>()
            .Property(definition => definition.OptionsJson)
            .HasConversion(jsonDocumentConverter);

        modelBuilder.Entity<CustomFieldDefinition>()
            .Property(definition => definition.DefaultValueJson)
            .HasConversion(jsonDocumentConverter);

        modelBuilder.Entity<CustomFieldDefinition>()
            .Property(definition => definition.ValidationJson)
            .HasConversion(jsonDocumentConverter);
    }
}

using algo.Application.Abstractions;
using algo.Domain.Catalog.Entities;
using algo.Domain.Storage.Entities;
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

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<AccessPolicyEntity> AccessPolicies => Set<AccessPolicyEntity>();

    public DbSet<CustomFieldDefinition> CustomFieldDefinitions => Set<CustomFieldDefinition>();

    public new DbSet<ApplicationRole> Roles => Set<ApplicationRole>();

    public DbSet<OtpToken> OtpTokens => Set<OtpToken>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<ApplicationLog> ApplicationLogs => Set<ApplicationLog>();

    public DbSet<ErrorLog> ErrorLogs => Set<ErrorLog>();

    public DbSet<StorageConfiguration> StorageConfigurations => Set<StorageConfiguration>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(category => category.Id);
            entity.HasIndex(category => category.Name).IsUnique();
            entity.Property(category => category.Name).HasMaxLength(256).IsRequired();
            entity.Property(category => category.Description).HasMaxLength(2000);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(product => product.Id);
            entity.Property(product => product.Name).HasMaxLength(256).IsRequired();
            entity.Property(product => product.PriceUsd).HasPrecision(18, 2);
            entity.Property(product => product.PriceSyp).HasPrecision(18, 2);
            entity.Property(product => product.DiscountedPriceUsd).HasPrecision(18, 2);
            entity.Property(product => product.DiscountedPriceSyp).HasPrecision(18, 2);
            entity.Property(product => product.ExternalGameId).HasMaxLength(128);
            entity.Property(product => product.Provider).HasMaxLength(256);
            entity.Property(product => product.ImageUrl).HasMaxLength(2048);
            entity.HasOne(product => product.Category)
                .WithMany(category => category.Products)
                .HasForeignKey(product => product.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<StorageConfiguration>(entity =>
        {
            entity.HasKey(configuration => configuration.Id);
            entity.Property(configuration => configuration.EndpointUrl).HasMaxLength(512).IsRequired();
            entity.Property(configuration => configuration.AccessKey).HasMaxLength(256).IsRequired();
            entity.Property(configuration => configuration.SecretKey).HasMaxLength(512).IsRequired();
            entity.Property(configuration => configuration.BucketName).HasMaxLength(256).IsRequired();
            entity.Property(configuration => configuration.Region).HasMaxLength(64).IsRequired();
            entity.Property(configuration => configuration.Folder).HasMaxLength(512).IsRequired();
            entity.Property(configuration => configuration.ScannerProvider).HasMaxLength(64).IsRequired();
            entity.Property(configuration => configuration.ScannerEndpointUrl).HasMaxLength(512);
            entity.Property(configuration => configuration.ScannerApiKey).HasMaxLength(512);
            entity.Property(configuration => configuration.QuarantineFolder).HasMaxLength(512);
        });

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

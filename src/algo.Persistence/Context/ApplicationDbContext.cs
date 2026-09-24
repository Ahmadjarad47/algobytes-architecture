using algo.Application.Abstractions;
using algo.Domain.Catalog.Entities;
using algo.Domain.Storage.Entities;
using algo.Domain.CustomFields;
using algo.Domain.Identity.Entities;
using algo.Domain.Logging.Entities;
using algo.Domain.Sales.Entities;
using System.Text.Json;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using AccessPolicyEntity = algo.Domain.Identity.Policies.AccessPolicy;

namespace algo.Persistence.Context;

public sealed class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>, IApplicationDbContext
{
    private static readonly ValueConverter<JsonDocument?, string?> JsonDocumentValueConverter = new(
        document => document == null ? null : document.RootElement.GetRawText(),
        json => string.IsNullOrWhiteSpace(json) ? null : JsonDocument.Parse(json));

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    public DbSet<Payment> Payments => Set<Payment>();

    public DbSet<WalletTransaction> WalletTransactions => Set<WalletTransaction>();

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
            entity.Property(category => category.ImageUrl).HasMaxLength(2048);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(product => product.Id);
            entity.Property(product => product.Name).HasMaxLength(256).IsRequired();
            entity.Property(product => product.CurrencyCode).HasMaxLength(8).IsRequired();
            entity.Property(product => product.Price).HasPrecision(18, 2);
            entity.Property(product => product.DiscountedPrice).HasPrecision(18, 2);
            entity.Property(product => product.PriceUsd).HasPrecision(18, 2);
            entity.Property(product => product.PriceSyp).HasPrecision(18, 2);
            entity.Property(product => product.DiscountedPriceUsd).HasPrecision(18, 2);
            entity.Property(product => product.DiscountedPriceSyp).HasPrecision(18, 2);
            entity.Property(product => product.ExternalGameId).HasMaxLength(128);
            entity.Property(product => product.Provider).HasMaxLength(256);
            entity.Property(product => product.ImageUrl).HasMaxLength(2048);
            ConfigureJsonDocumentProperty(entity.Property(product => product.CustomFields));
            entity.HasOne(product => product.Category)
                .WithMany(category => category.Products)
                .HasForeignKey(product => product.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(order => order.Id);
            entity.HasIndex(order => order.OrderNumber).IsUnique();
            entity.Property(order => order.OrderNumber).HasMaxLength(128).IsRequired();
            entity.Property(order => order.CurrencyCode).HasMaxLength(8).IsRequired();
            entity.Property(order => order.TotalAmount).HasPrecision(18, 2);
            entity.Property(order => order.ExchangeRateUsedToBase).HasPrecision(18, 6);
            entity.Property(order => order.PaymentMethod).HasMaxLength(64);
            entity.Property(order => order.OrderStatus).HasMaxLength(32).IsRequired();
            entity.Property(order => order.UserId).HasMaxLength(450).IsRequired();
            entity.Property(order => order.CreatedAt).IsRequired();
            ConfigureJsonDocumentProperty(entity.Property(order => order.CustomFields));
            entity.HasOne(order => order.User)
                .WithMany(user => user.Orders)
                .HasForeignKey(order => order.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(orderItem => orderItem.Id);
            entity.Property(orderItem => orderItem.UnitPrice).HasPrecision(18, 2);
            entity.HasOne(orderItem => orderItem.Order)
                .WithMany(order => order.OrderItems)
                .HasForeignKey(orderItem => orderItem.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(orderItem => orderItem.Product)
                .WithMany()
                .HasForeignKey(orderItem => orderItem.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(payment => payment.Id);
            entity.Property(payment => payment.CurrencyCode).HasMaxLength(8).IsRequired();
            entity.Property(payment => payment.GatewayName).HasMaxLength(128).IsRequired();
            entity.Property(payment => payment.GatewayTransactionId).HasMaxLength(256).IsRequired();
            entity.Property(payment => payment.Amount).HasPrecision(18, 2);
            entity.Property(payment => payment.PaymentStatus).HasMaxLength(32).IsRequired();
            entity.HasOne(payment => payment.Order)
                .WithMany(order => order.Payments)
                .HasForeignKey(payment => payment.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WalletTransaction>(entity =>
        {
            entity.HasKey(walletTransaction => walletTransaction.Id);
            entity.Property(walletTransaction => walletTransaction.UserId).HasMaxLength(450).IsRequired();
            entity.Property(walletTransaction => walletTransaction.CurrencyCode).HasMaxLength(8).IsRequired();
            entity.Property(walletTransaction => walletTransaction.Amount).HasPrecision(18, 2);
            entity.Property(walletTransaction => walletTransaction.TransactionType).HasMaxLength(32).IsRequired();
            entity.Property(walletTransaction => walletTransaction.Description).HasMaxLength(1000);
            entity.Property(walletTransaction => walletTransaction.ReferenceId).HasMaxLength(128);
            entity.Property(walletTransaction => walletTransaction.CreatedAt).IsRequired();
            entity.HasOne(walletTransaction => walletTransaction.User)
                .WithMany(user => user.WalletTransactions)
                .HasForeignKey(walletTransaction => walletTransaction.UserId)
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
        modelBuilder.Entity<ApplicationUser>()
            .Property(user => user.CustomFields)
            .HasConversion(JsonDocumentValueConverter);

        modelBuilder.Entity<ApplicationRole>()
            .Property(role => role.CustomFields)
            .HasConversion(JsonDocumentValueConverter);

        modelBuilder.Entity<AccessPolicyEntity>()
            .Property(policy => policy.CustomFields)
            .HasConversion(JsonDocumentValueConverter);

        modelBuilder.Entity<Product>()
            .Property(product => product.CustomFields)
            .HasConversion(JsonDocumentValueConverter);

        modelBuilder.Entity<Order>()
            .Property(order => order.CustomFields)
            .HasConversion(JsonDocumentValueConverter);

        modelBuilder.Entity<CustomFieldDefinition>()
            .Property(definition => definition.OptionsJson)
            .HasConversion(JsonDocumentValueConverter);

        modelBuilder.Entity<CustomFieldDefinition>()
            .Property(definition => definition.DefaultValueJson)
            .HasConversion(JsonDocumentValueConverter);

        modelBuilder.Entity<CustomFieldDefinition>()
            .Property(definition => definition.ValidationJson)
            .HasConversion(JsonDocumentValueConverter);
    }

    private void ConfigureJsonDocumentProperty(PropertyBuilder<JsonDocument?> property)
    {
        if (Database.IsNpgsql())
        {
            property.HasColumnType("jsonb");
            return;
        }

        property.HasConversion(JsonDocumentValueConverter);
    }
}

using algo.Domain.Catalog.Entities;
using algo.Domain.CustomFields;
using algo.Domain.Identity.Entities;
using algo.Domain.Identity.Policies;
using algo.Domain.Logging.Entities;
using algo.Domain.Storage.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace algo.Application.Abstractions.Persistence;

public interface IApplicationDbContext
{
    DbSet<Category> Categories { get; }

    DbSet<Product> Products { get; }

    DbSet<ApplicationUser> Users { get; }

    DbSet<ApplicationRole> Roles { get; }

    DbSet<IdentityUserRole<string>> UserRoles { get; }

    DbSet<AccessPolicy> AccessPolicies { get; }

    DbSet<CustomFieldDefinition> CustomFieldDefinitions { get; }

    DbSet<OtpToken> OtpTokens { get; }

    DbSet<RefreshToken> RefreshTokens { get; }

    DbSet<ApplicationLog> ApplicationLogs { get; }

    DbSet<ErrorLog> ErrorLogs { get; }

    DbSet<StorageConfiguration> StorageConfigurations { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}


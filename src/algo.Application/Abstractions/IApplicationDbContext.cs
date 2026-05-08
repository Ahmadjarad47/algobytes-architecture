using algo.Domain.Identity.Entities;
using algo.Domain.Identity.Policies;
using algo.Domain.Logging.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace algo.Application.Abstractions;

public interface IApplicationDbContext
{
    DbSet<ApplicationUser> Users { get; }

    DbSet<IdentityRole> Roles { get; }

    DbSet<IdentityUserRole<string>> UserRoles { get; }

    DbSet<AccessPolicy> AccessPolicies { get; }

    DbSet<OtpToken> OtpTokens { get; }

    DbSet<RefreshToken> RefreshTokens { get; }

    DbSet<ApplicationLog> ApplicationLogs { get; }

    DbSet<ErrorLog> ErrorLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
using algo.Application.Abstractions;
using algo.Domain.Identity.Entities;
using algo.Domain.Logging.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using AccessPolicyEntity = algo.Domain.Identity.Policies.AccessPolicy;

namespace algo.Persistence.Context;

public sealed class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<AccessPolicyEntity> AccessPolicies => Set<AccessPolicyEntity>();

    public DbSet<OtpToken> OtpTokens => Set<OtpToken>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<ApplicationLog> ApplicationLogs => Set<ApplicationLog>();

    public DbSet<ErrorLog> ErrorLogs => Set<ErrorLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}

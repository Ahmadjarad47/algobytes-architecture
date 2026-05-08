using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AccessPolicyEntity = algo.Domain.Identity.Policies.AccessPolicy;

namespace algo.Persistence.Configurations;

internal sealed class AccessPolicyConfiguration : IEntityTypeConfiguration<AccessPolicyEntity>
{
    public void Configure(EntityTypeBuilder<AccessPolicyEntity> builder)
    {
        builder.ToTable("access_policies");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Resource).HasMaxLength(128).IsRequired();
        builder.Property(p => p.Action).HasMaxLength(64).IsRequired();
        builder.Property(p => p.Effect).HasConversion<int>().IsRequired();
        builder.Property(p => p.SubjectType).HasConversion<int>().IsRequired();
        builder.Property(p => p.SubjectKey).HasMaxLength(256).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(1024);
        builder.Property(p => p.CreatedByUserId).HasMaxLength(450);
        builder.Property(p => p.UpdatedByUserId).HasMaxLength(450);

        builder.HasIndex(p => new { p.Resource, p.Action, p.IsEnabled });
        builder.HasIndex(p => new { p.SubjectType, p.SubjectKey, p.IsEnabled });
    }
}

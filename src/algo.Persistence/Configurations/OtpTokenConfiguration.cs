using algo.Domain.Identity.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace algo.Persistence.Configurations;

internal sealed class OtpTokenConfiguration : IEntityTypeConfiguration<OtpToken>
{
    public void Configure(EntityTypeBuilder<OtpToken> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.UserId).IsRequired();
        builder.Property(t => t.CodeHash).HasMaxLength(128).IsRequired();
        builder.Property(t => t.Purpose).HasConversion<int>();

        builder.HasIndex(t => new { t.UserId, t.Purpose });

        builder.HasOne(t => t.User)
            .WithMany(u => u.OtpTokens)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

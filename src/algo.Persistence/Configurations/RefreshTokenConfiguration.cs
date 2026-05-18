using algo.Domain.Identity.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace algo.Persistence.Configurations;

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.HasQueryFilter(token => token.User.DeletedAt == null && token.User.TrashedAt == null);

        builder.HasKey(t => t.Id);

        builder.Property(t => t.UserId).IsRequired();
        builder.Property(t => t.TokenHash).HasMaxLength(128).IsRequired();
        builder.Property(t => t.ReplacedByTokenHash).HasMaxLength(128);
        builder.Property(t => t.IpAddress).HasMaxLength(64);
        builder.Property(t => t.Location).HasMaxLength(160);
        builder.Property(t => t.Device).HasMaxLength(80);
        builder.Property(t => t.Browser).HasMaxLength(80);
        builder.Property(t => t.OperatingSystem).HasMaxLength(80);
        builder.Property(t => t.UserAgent).HasMaxLength(512);
        builder.Property(t => t.RevokedByUserId).HasMaxLength(450);

        builder.HasIndex(t => t.TokenHash).IsUnique();
        builder.HasIndex(t => t.UserId);
        builder.HasIndex(t => t.RevokedAt);
        builder.HasIndex(t => t.ExpiresAt);

        builder.HasOne(t => t.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

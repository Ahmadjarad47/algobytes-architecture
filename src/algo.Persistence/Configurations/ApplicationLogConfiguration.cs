using algo.Domain.Logging.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace algo.Persistence.Configurations;

internal sealed class ApplicationLogConfiguration : IEntityTypeConfiguration<ApplicationLog>
{
    public void Configure(EntityTypeBuilder<ApplicationLog> builder)
    {
        builder.ToTable("ApplicationLogs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).UseIdentityByDefaultColumn();

        builder.Property(x => x.Timestamp).IsRequired();

        builder.Property(x => x.Level).HasMaxLength(64).IsRequired();

        builder.Property(x => x.Message).IsRequired();

        builder.Property(x => x.MessageTemplate).HasMaxLength(4096);

        builder.Property(x => x.Exception);

        builder.Property(x => x.Properties);

        builder.Property(x => x.TraceId).HasMaxLength(128);

        builder.Property(x => x.UserId).HasMaxLength(450);

        builder.Property(x => x.UserName).HasMaxLength(256);

        builder.Property(x => x.RequestPath).HasMaxLength(2048);

        builder.Property(x => x.RequestMethod).HasMaxLength(16);

        builder.HasIndex(x => x.Timestamp);

        builder.HasIndex(x => x.Level);

        builder.HasIndex(x => x.TraceId);

        builder.HasIndex(x => x.UserId);
    }
}

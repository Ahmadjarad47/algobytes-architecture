using algo.Domain.Logging.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace algo.Persistence.Configurations;

internal sealed class ErrorLogConfiguration : IEntityTypeConfiguration<ErrorLog>
{
    public void Configure(EntityTypeBuilder<ErrorLog> builder)
    {
        builder.ToTable("ErrorLogs");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityByDefaultColumn();

        builder.Property(x => x.Timestamp).IsRequired();
        builder.Property(x => x.Level).HasMaxLength(32).IsRequired();
        builder.Property(x => x.ExceptionType).HasMaxLength(512).IsRequired();
        builder.Property(x => x.Message).IsRequired();
        builder.Property(x => x.Source).HasMaxLength(1024);
        builder.Property(x => x.Path).HasMaxLength(2048);
        builder.Property(x => x.Method).HasMaxLength(16);
        builder.Property(x => x.StatusCode).IsRequired();
        builder.Property(x => x.TraceId).HasMaxLength(128);
        builder.Property(x => x.UserId).HasMaxLength(450);
        builder.Property(x => x.UserName).HasMaxLength(256);
        builder.Property(x => x.Environment).HasMaxLength(128).IsRequired();
        builder.Property(x => x.MachineName).HasMaxLength(256).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasIndex(x => x.Timestamp);
        builder.HasIndex(x => x.ExceptionType);
        builder.HasIndex(x => x.StatusCode);
        builder.HasIndex(x => x.TraceId);
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.UserName);
    }
}

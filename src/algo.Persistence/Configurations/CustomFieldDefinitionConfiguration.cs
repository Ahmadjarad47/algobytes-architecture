using algo.Domain.CustomFields;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace algo.Persistence.Configurations;

internal sealed class CustomFieldDefinitionConfiguration : IEntityTypeConfiguration<CustomFieldDefinition>
{
    public void Configure(EntityTypeBuilder<CustomFieldDefinition> builder)
    {
        builder.ToTable("custom_field_definitions");

        builder.HasKey(definition => definition.Id);

        builder.Property(definition => definition.Entity)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(definition => definition.Key)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(definition => definition.Label)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(definition => definition.Type)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(definition => definition.OptionsJson);

        builder.Property(definition => definition.DefaultValueJson);

        builder.Property(definition => definition.ValidationJson);

        builder.HasIndex(definition => new { definition.Entity, definition.Key })
            .IsUnique();
    }
}

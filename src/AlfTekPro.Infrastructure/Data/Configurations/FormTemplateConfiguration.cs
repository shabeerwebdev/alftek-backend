using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AlfTekPro.Domain.Entities.Platform;

namespace AlfTekPro.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for FormTemplate entity
/// Configures JSONB storage for dynamic form schemas
/// </summary>
public class FormTemplateConfiguration : IEntityTypeConfiguration<FormTemplate>
{
    public void Configure(EntityTypeBuilder<FormTemplate> builder)
    {
        // Table name
        builder.ToTable("form_templates");

        // Primary key
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id)
            .HasColumnName("id");

        // Properties
        builder.Property(f => f.RegionId)
            .IsRequired()
            .HasColumnName("region_id");

        builder.Property(f => f.Module)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnName("module");

        // CRITICAL: JSONB column for PostgreSQL
        builder.Property(f => f.SchemaJson)
            .IsRequired()
            .HasColumnType("jsonb")
            .HasColumnName("schema_json");

        builder.Property(f => f.IsActive)
            .IsRequired()
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(f => f.CreatedAt)
            .IsRequired()
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(f => f.UpdatedAt)
            .HasColumnName("updated_at");

        // Indexes
        builder.HasIndex(f => new { f.RegionId, f.Module })
            .IsUnique()
            .HasDatabaseName("ix_form_templates_region_id_module");

        builder.HasIndex(f => f.RegionId)
            .HasDatabaseName("ix_form_templates_region_id");

        builder.HasIndex(f => f.IsActive)
            .HasDatabaseName("ix_form_templates_is_active");

        // GIN index for JSONB column (PostgreSQL specific - for efficient JSON queries)
        builder.HasIndex(f => f.SchemaJson)
            .HasMethod("gin")
            .HasDatabaseName("ix_form_templates_schema_json_gin");

        // Relationships (Region relationship is configured in RegionConfiguration)
    }
}

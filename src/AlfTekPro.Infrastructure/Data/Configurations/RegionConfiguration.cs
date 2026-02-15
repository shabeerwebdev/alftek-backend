using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AlfTekPro.Domain.Entities.Platform;

namespace AlfTekPro.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for Region entity
/// Configures table structure, constraints, and relationships
/// </summary>
public class RegionConfiguration : IEntityTypeConfiguration<Region>
{
    public void Configure(EntityTypeBuilder<Region> builder)
    {
        // Table name (lowercase with snake_case for PostgreSQL convention)
        builder.ToTable("regions");

        // Primary key
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id)
            .HasColumnName("id");

        // Properties
        builder.Property(r => r.Code)
            .IsRequired()
            .HasMaxLength(10)
            .HasColumnName("code");

        builder.Property(r => r.Name)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnName("name");

        builder.Property(r => r.CurrencyCode)
            .IsRequired()
            .HasMaxLength(3)
            .HasColumnName("currency_code");

        builder.Property(r => r.DateFormat)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("date_format");

        builder.Property(r => r.Direction)
            .IsRequired()
            .HasMaxLength(3)
            .HasColumnName("direction")
            .HasDefaultValue("ltr");

        builder.Property(r => r.LanguageCode)
            .IsRequired()
            .HasMaxLength(10)
            .HasColumnName("language_code")
            .HasDefaultValue("en");

        builder.Property(r => r.Timezone)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnName("timezone");

        builder.Property(r => r.CreatedAt)
            .IsRequired()
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(r => r.UpdatedAt)
            .HasColumnName("updated_at");

        // Indexes
        builder.HasIndex(r => r.Code)
            .IsUnique()
            .HasDatabaseName("ix_regions_code");

        // Relationships
        builder.HasMany(r => r.Tenants)
            .WithOne(t => t.Region)
            .HasForeignKey(t => t.RegionId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_tenants_region_id");

        builder.HasMany(r => r.FormTemplates)
            .WithOne(f => f.Region)
            .HasForeignKey(f => f.RegionId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_form_templates_region_id");
    }
}

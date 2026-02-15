using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AlfTekPro.Domain.Entities.CoreHR;

namespace AlfTekPro.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for Designation entity
/// </summary>
public class DesignationConfiguration : IEntityTypeConfiguration<Designation>
{
    public void Configure(EntityTypeBuilder<Designation> builder)
    {
        // Table name
        builder.ToTable("designations");

        // Primary key
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id)
            .HasColumnName("id");

        // Tenant isolation
        builder.Property(d => d.TenantId)
            .IsRequired()
            .HasColumnName("tenant_id");

        // Properties
        builder.Property(d => d.Title)
            .IsRequired()
            .HasMaxLength(200)
            .HasColumnName("title");

        builder.Property(d => d.Code)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("code");

        builder.Property(d => d.Description)
            .HasMaxLength(500)
            .HasColumnName("description");

        builder.Property(d => d.Level)
            .IsRequired()
            .HasColumnName("level");

        builder.Property(d => d.IsActive)
            .IsRequired()
            .HasDefaultValue(true)
            .HasColumnName("is_active");

        builder.Property(d => d.CreatedAt)
            .IsRequired()
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(d => d.UpdatedAt)
            .HasColumnName("updated_at");

        // Indexes
        builder.HasIndex(d => d.TenantId)
            .HasDatabaseName("ix_designations_tenant_id");

        builder.HasIndex(d => new { d.TenantId, d.Title })
            .IsUnique()
            .HasDatabaseName("ix_designations_tenant_id_title");

        builder.HasIndex(d => new { d.TenantId, d.Level })
            .HasDatabaseName("ix_designations_tenant_id_level");
    }
}

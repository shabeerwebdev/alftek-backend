using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AlfTekPro.Domain.Entities.Platform;

namespace AlfTekPro.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for Tenant entity
/// Configures multi-tenant organization records
/// </summary>
public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        // Table name
        builder.ToTable("tenants");

        // Primary key
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id)
            .HasColumnName("id");

        // Properties
        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(255)
            .HasColumnName("name");

        builder.Property(t => t.Subdomain)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnName("subdomain");

        builder.Property(t => t.RegionId)
            .IsRequired()
            .HasColumnName("region_id");

        builder.Property(t => t.IsActive)
            .IsRequired()
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(t => t.SubscriptionStart)
            .IsRequired()
            .HasColumnName("subscription_start");

        builder.Property(t => t.SubscriptionEnd)
            .HasColumnName("subscription_end");

        builder.Property(t => t.CreatedAt)
            .IsRequired()
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(t => t.UpdatedAt)
            .HasColumnName("updated_at");

        // Indexes
        builder.HasIndex(t => t.Subdomain)
            .IsUnique()
            .HasDatabaseName("ix_tenants_subdomain");

        builder.HasIndex(t => t.RegionId)
            .HasDatabaseName("ix_tenants_region_id");

        builder.HasIndex(t => t.IsActive)
            .HasDatabaseName("ix_tenants_is_active");

        // Relationships (Region relationship is configured in RegionConfiguration)
        builder.HasMany(t => t.Users)
            .WithOne(u => u.Tenant)
            .HasForeignKey(u => u.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_users_tenant_id");
    }
}

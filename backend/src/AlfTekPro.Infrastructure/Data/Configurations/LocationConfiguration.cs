using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AlfTekPro.Domain.Entities.CoreHR;

namespace AlfTekPro.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for Location entity
/// Includes geofencing coordinates
/// </summary>
public class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    public void Configure(EntityTypeBuilder<Location> builder)
    {
        // Table name
        builder.ToTable("locations");

        // Primary key
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id)
            .HasColumnName("id");

        // Tenant isolation
        builder.Property(l => l.TenantId)
            .IsRequired()
            .HasColumnName("tenant_id");

        // Properties
        builder.Property(l => l.Name)
            .IsRequired()
            .HasMaxLength(200)
            .HasColumnName("name");

        builder.Property(l => l.Code)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("code");

        builder.Property(l => l.Address)
            .HasMaxLength(500)
            .HasColumnName("address");

        builder.Property(l => l.City)
            .HasMaxLength(100)
            .HasColumnName("city");

        builder.Property(l => l.State)
            .HasMaxLength(100)
            .HasColumnName("state");

        builder.Property(l => l.Country)
            .HasMaxLength(100)
            .HasColumnName("country");

        builder.Property(l => l.PostalCode)
            .HasMaxLength(20)
            .HasColumnName("postal_code");

        builder.Property(l => l.ContactPhone)
            .HasMaxLength(50)
            .HasColumnName("contact_phone");

        builder.Property(l => l.ContactEmail)
            .HasMaxLength(200)
            .HasColumnName("contact_email");

        builder.Property(l => l.Latitude)
            .HasColumnType("decimal(10,7)")
            .HasColumnName("latitude");

        builder.Property(l => l.Longitude)
            .HasColumnType("decimal(10,7)")
            .HasColumnName("longitude");

        builder.Property(l => l.RadiusMeters)
            .HasColumnName("radius_meters")
            .HasDefaultValue(100);

        builder.Property(l => l.IsActive)
            .IsRequired()
            .HasDefaultValue(true)
            .HasColumnName("is_active");

        builder.Property(l => l.CreatedAt)
            .IsRequired()
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(l => l.UpdatedAt)
            .HasColumnName("updated_at");

        // Indexes
        builder.HasIndex(l => l.TenantId)
            .HasDatabaseName("ix_locations_tenant_id");

        builder.HasIndex(l => new { l.TenantId, l.Name })
            .HasDatabaseName("ix_locations_tenant_id_name");

        // Spatial index for geofencing (if needed in future)
        // builder.HasIndex(l => new { l.Latitude, l.Longitude })
        //     .HasDatabaseName("ix_locations_coordinates");
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AlfTekPro.Domain.Entities.Workforce;

namespace AlfTekPro.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for ShiftMaster entity
/// </summary>
public class ShiftMasterConfiguration : IEntityTypeConfiguration<ShiftMaster>
{
    public void Configure(EntityTypeBuilder<ShiftMaster> builder)
    {
        // Table name
        builder.ToTable("shift_masters");

        // Primary key
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id)
            .HasColumnName("id");

        // Tenant isolation
        builder.Property(s => s.TenantId)
            .IsRequired()
            .HasColumnName("tenant_id");

        // Properties
        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(200)
            .HasColumnName("name");

        builder.Property(s => s.Code)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("code");

        builder.Property(s => s.StartTime)
            .IsRequired()
            .HasColumnName("start_time");

        builder.Property(s => s.EndTime)
            .IsRequired()
            .HasColumnName("end_time");

        builder.Property(s => s.GracePeriodMinutes)
            .IsRequired()
            .HasColumnName("grace_period_mins")
            .HasDefaultValue(15);

        builder.Property(s => s.TotalHours)
            .IsRequired()
            .HasColumnType("decimal(4,2)")
            .HasColumnName("total_hours");

        builder.Property(s => s.IsActive)
            .IsRequired()
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(s => s.CreatedAt)
            .IsRequired()
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(s => s.UpdatedAt)
            .HasColumnName("updated_at");

        // Indexes
        builder.HasIndex(s => s.TenantId)
            .HasDatabaseName("ix_shift_masters_tenant_id");

        builder.HasIndex(s => new { s.TenantId, s.Code })
            .IsUnique()
            .HasDatabaseName("ix_shift_masters_tenant_id_code");

        builder.HasIndex(s => new { s.TenantId, s.IsActive })
            .HasDatabaseName("ix_shift_masters_tenant_id_is_active");

        // Relationships
        builder.HasMany(s => s.RosterEntries)
            .WithOne(re => re.Shift)
            .HasForeignKey(re => re.ShiftId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_employee_rosters_shift_id");
    }
}

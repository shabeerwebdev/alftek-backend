using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AlfTekPro.Domain.Entities.Workforce;
using AlfTekPro.Domain.Enums;

namespace AlfTekPro.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for AttendanceLog entity
/// </summary>
public class AttendanceLogConfiguration : IEntityTypeConfiguration<AttendanceLog>
{
    public void Configure(EntityTypeBuilder<AttendanceLog> builder)
    {
        // Table name
        builder.ToTable("attendance_logs");

        // Primary key
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id)
            .HasColumnName("id");

        // Tenant isolation
        builder.Property(a => a.TenantId)
            .IsRequired()
            .HasColumnName("tenant_id");

        // Properties
        builder.Property(a => a.EmployeeId)
            .IsRequired()
            .HasColumnName("employee_id");

        builder.Property(a => a.Date)
            .IsRequired()
            .HasColumnName("date");

        builder.Property(a => a.ClockIn)
            .HasColumnName("clock_in");

        builder.Property(a => a.ClockInIp)
            .HasMaxLength(45) // IPv6 max length
            .HasColumnName("clock_in_ip");

        builder.Property(a => a.ClockInLatitude)
            .HasColumnType("decimal(10,7)")
            .HasColumnName("clock_in_latitude");

        builder.Property(a => a.ClockInLongitude)
            .HasColumnType("decimal(10,7)")
            .HasColumnName("clock_in_longitude");

        builder.Property(a => a.ClockOut)
            .HasColumnName("clock_out");

        builder.Property(a => a.ClockOutIp)
            .HasMaxLength(45)
            .HasColumnName("clock_out_ip");

        builder.Property(a => a.Status)
            .IsRequired()
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(a => a.IsLate)
            .IsRequired()
            .HasColumnName("is_late")
            .HasDefaultValue(false);

        builder.Property(a => a.LateByMinutes)
            .IsRequired()
            .HasColumnName("late_by_minutes")
            .HasDefaultValue(0);

        builder.Property(a => a.IsRegularized)
            .IsRequired()
            .HasColumnName("is_regularized")
            .HasDefaultValue(false);

        builder.Property(a => a.RegularizationReason)
            .HasMaxLength(500)
            .HasColumnName("regularization_reason");

        builder.Property(a => a.CreatedAt)
            .IsRequired()
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(a => a.UpdatedAt)
            .HasColumnName("updated_at");

        // Indexes
        builder.HasIndex(a => a.TenantId)
            .HasDatabaseName("ix_attendance_logs_tenant_id");

        builder.HasIndex(a => a.EmployeeId)
            .HasDatabaseName("ix_attendance_logs_employee_id");

        // Unique constraint: one attendance record per employee per day
        builder.HasIndex(a => new { a.EmployeeId, a.Date })
            .IsUnique()
            .HasDatabaseName("ix_attendance_logs_employee_id_date");

        builder.HasIndex(a => new { a.TenantId, a.Date })
            .HasDatabaseName("ix_attendance_logs_tenant_id_date");

        builder.HasIndex(a => new { a.TenantId, a.Status })
            .HasDatabaseName("ix_attendance_logs_tenant_id_status");

        builder.HasIndex(a => new { a.EmployeeId, a.IsLate })
            .HasDatabaseName("ix_attendance_logs_employee_id_is_late");

        // Relationships (Employee relationship configured in EmployeeConfiguration)
    }
}

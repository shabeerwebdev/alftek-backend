using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AlfTekPro.Domain.Entities.Workforce;

namespace AlfTekPro.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for EmployeeRoster entity
/// </summary>
public class EmployeeRosterConfiguration : IEntityTypeConfiguration<EmployeeRoster>
{
    public void Configure(EntityTypeBuilder<EmployeeRoster> builder)
    {
        // Table name
        builder.ToTable("employee_rosters");

        // Primary key
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id)
            .HasColumnName("id");

        // Tenant isolation
        builder.Property(r => r.TenantId)
            .IsRequired()
            .HasColumnName("tenant_id");

        // Properties
        builder.Property(r => r.EmployeeId)
            .IsRequired()
            .HasColumnName("employee_id");

        builder.Property(r => r.ShiftId)
            .IsRequired()
            .HasColumnName("shift_id");

        builder.Property(r => r.EffectiveDate)
            .IsRequired()
            .HasColumnName("effective_date");

        builder.Property(r => r.CreatedAt)
            .IsRequired()
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(r => r.UpdatedAt)
            .HasColumnName("updated_at");

        // Indexes
        builder.HasIndex(r => r.TenantId)
            .HasDatabaseName("ix_employee_rosters_tenant_id");

        builder.HasIndex(r => r.EmployeeId)
            .HasDatabaseName("ix_employee_rosters_employee_id");

        builder.HasIndex(r => new { r.EmployeeId, r.EffectiveDate })
            .HasDatabaseName("ix_employee_rosters_employee_id_effective_date");

        builder.HasIndex(r => r.ShiftId)
            .HasDatabaseName("ix_employee_rosters_shift_id");

        // Relationships (Employee and Shift relationships configured in their respective configs)
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AlfTekPro.Domain.Entities.CoreHR;

namespace AlfTekPro.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for EmployeeJobHistory entity
/// Implements SCD Type 2 temporal tracking
/// </summary>
public class EmployeeJobHistoryConfiguration : IEntityTypeConfiguration<EmployeeJobHistory>
{
    public void Configure(EntityTypeBuilder<EmployeeJobHistory> builder)
    {
        // Table name
        builder.ToTable("employee_job_histories");

        // Primary key
        builder.HasKey(jh => jh.Id);
        builder.Property(jh => jh.Id)
            .HasColumnName("id");

        // Tenant isolation
        builder.Property(jh => jh.TenantId)
            .IsRequired()
            .HasColumnName("tenant_id");

        // Properties
        builder.Property(jh => jh.EmployeeId)
            .IsRequired()
            .HasColumnName("employee_id");

        builder.Property(jh => jh.DepartmentId)
            .HasColumnName("department_id");

        builder.Property(jh => jh.DesignationId)
            .HasColumnName("designation_id");

        builder.Property(jh => jh.ReportingManagerId)
            .HasColumnName("reporting_manager_id");

        builder.Property(jh => jh.LocationId)
            .HasColumnName("location_id");

        builder.Property(jh => jh.SalaryTierId)
            .HasColumnName("salary_tier_id");

        builder.Property(jh => jh.ValidFrom)
            .IsRequired()
            .HasColumnName("valid_from");

        builder.Property(jh => jh.ValidTo)
            .HasColumnName("valid_to");

        builder.Property(jh => jh.ChangeType)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("change_type");

        builder.Property(jh => jh.ChangeReason)
            .HasMaxLength(500)
            .HasColumnName("change_reason");

        builder.Property(jh => jh.CreatedBy)
            .HasColumnName("created_by");

        builder.Property(jh => jh.CreatedAt)
            .IsRequired()
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(jh => jh.UpdatedAt)
            .HasColumnName("updated_at");

        // Indexes
        builder.HasIndex(jh => jh.TenantId)
            .HasDatabaseName("ix_employee_job_histories_tenant_id");

        builder.HasIndex(jh => jh.EmployeeId)
            .HasDatabaseName("ix_employee_job_histories_employee_id");

        // Index for current job query (valid_to IS NULL)
        builder.HasIndex(jh => new { jh.EmployeeId, jh.ValidTo })
            .HasDatabaseName("ix_employee_job_histories_employee_id_valid_to");

        // Index for temporal queries
        builder.HasIndex(jh => new { jh.EmployeeId, jh.ValidFrom, jh.ValidTo })
            .HasDatabaseName("ix_employee_job_histories_employee_id_valid_from_valid_to");

        builder.HasIndex(jh => jh.DepartmentId)
            .HasDatabaseName("ix_employee_job_histories_department_id");

        builder.HasIndex(jh => jh.DesignationId)
            .HasDatabaseName("ix_employee_job_histories_designation_id");

        builder.HasIndex(jh => jh.ReportingManagerId)
            .HasDatabaseName("ix_employee_job_histories_reporting_manager_id");

        builder.HasIndex(jh => jh.LocationId)
            .HasDatabaseName("ix_employee_job_histories_location_id");

        // Relationships (Employee relationship configured in EmployeeConfiguration)
        builder.HasOne(jh => jh.Department)
            .WithMany(d => d.EmployeeJobHistories)
            .HasForeignKey(jh => jh.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_employee_job_histories_department_id");

        builder.HasOne(jh => jh.Designation)
            .WithMany(d => d.EmployeeJobHistories)
            .HasForeignKey(jh => jh.DesignationId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_employee_job_histories_designation_id");

        builder.HasOne(jh => jh.ReportingManager)
            .WithMany()
            .HasForeignKey(jh => jh.ReportingManagerId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_employee_job_histories_reporting_manager_id");

        builder.HasOne(jh => jh.Location)
            .WithMany(l => l.EmployeeJobHistories)
            .HasForeignKey(jh => jh.LocationId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_employee_job_histories_location_id");

        builder.HasOne(jh => jh.Creator)
            .WithMany()
            .HasForeignKey(jh => jh.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_employee_job_histories_created_by");

        // SalaryTier relationship will be configured in SalaryStructureConfiguration
    }
}

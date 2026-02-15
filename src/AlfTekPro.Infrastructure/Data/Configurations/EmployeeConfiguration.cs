using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AlfTekPro.Domain.Entities.CoreHR;
using AlfTekPro.Domain.Enums;

namespace AlfTekPro.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for Employee entity
/// Includes JSONB dynamic data storage
/// </summary>
public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        // Table name
        builder.ToTable("employees");

        // Primary key
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasColumnName("id");

        // Tenant isolation
        builder.Property(e => e.TenantId)
            .IsRequired()
            .HasColumnName("tenant_id");

        // Properties
        builder.Property(e => e.UserId)
            .HasColumnName("user_id");

        builder.Property(e => e.EmployeeCode)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("employee_code");

        builder.Property(e => e.FirstName)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnName("first_name");

        builder.Property(e => e.LastName)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnName("last_name");

        builder.Property(e => e.Email)
            .IsRequired()
            .HasMaxLength(255)
            .HasColumnName("email");

        builder.Property(e => e.Phone)
            .HasMaxLength(20)
            .HasColumnName("phone");

        builder.Property(e => e.DateOfBirth)
            .HasColumnName("date_of_birth");

        builder.Property(e => e.JoiningDate)
            .IsRequired()
            .HasColumnName("joining_date");

        builder.Property(e => e.Status)
            .IsRequired()
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(e => e.Gender)
            .HasMaxLength(20)
            .HasColumnName("gender");

        builder.Property(e => e.DepartmentId)
            .HasColumnName("department_id");

        builder.Property(e => e.DesignationId)
            .HasColumnName("designation_id");

        builder.Property(e => e.LocationId)
            .HasColumnName("location_id");

        builder.Property(e => e.ReportingManagerId)
            .HasColumnName("reporting_manager_id");

        // CRITICAL: JSONB column for region-specific dynamic data
        builder.Property(e => e.DynamicData)
            .HasColumnType("jsonb")
            .HasColumnName("dynamic_data");

        builder.Property(e => e.CreatedAt)
            .IsRequired()
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at");

        // Ignore computed property
        builder.Ignore(e => e.FullName);

        // Indexes
        builder.HasIndex(e => e.TenantId)
            .HasDatabaseName("ix_employees_tenant_id");

        builder.HasIndex(e => new { e.TenantId, e.EmployeeCode })
            .IsUnique()
            .HasDatabaseName("ix_employees_tenant_id_employee_code");

        builder.HasIndex(e => new { e.TenantId, e.Email })
            .HasDatabaseName("ix_employees_tenant_id_email");

        builder.HasIndex(e => e.UserId)
            .IsUnique()
            .HasDatabaseName("ix_employees_user_id")
            .HasFilter("user_id IS NOT NULL");

        builder.HasIndex(e => new { e.TenantId, e.Status })
            .HasDatabaseName("ix_employees_tenant_id_status");

        // GIN index for JSONB column (PostgreSQL specific)
        builder.HasIndex(e => e.DynamicData)
            .HasMethod("gin")
            .HasDatabaseName("ix_employees_dynamic_data_gin")
            .HasFilter("dynamic_data IS NOT NULL");

        // Full-text search index (PostgreSQL specific)
        // This will be added as a raw SQL migration step if needed

        // Relationships
        builder.HasOne(e => e.Department)
            .WithMany()
            .HasForeignKey(e => e.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_employees_department_id");

        builder.HasOne(e => e.Designation)
            .WithMany()
            .HasForeignKey(e => e.DesignationId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_employees_designation_id");

        builder.HasOne(e => e.Location)
            .WithMany()
            .HasForeignKey(e => e.LocationId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_employees_location_id");

        builder.HasOne(e => e.ReportingManager)
            .WithMany(e => e.DirectReports)
            .HasForeignKey(e => e.ReportingManagerId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_employees_reporting_manager_id");

        builder.HasOne(e => e.User)
            .WithOne(u => u.Employee)
            .HasForeignKey<Employee>(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_employees_user_id");

        builder.HasMany(e => e.JobHistories)
            .WithOne(jh => jh.Employee)
            .HasForeignKey(jh => jh.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_employee_job_histories_employee_id");

        builder.HasMany(e => e.AttendanceLogs)
            .WithOne(al => al.Employee)
            .HasForeignKey(al => al.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_attendance_logs_employee_id");

        builder.HasMany(e => e.LeaveBalances)
            .WithOne(lb => lb.Employee)
            .HasForeignKey(lb => lb.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_leave_balances_employee_id");

        builder.HasMany(e => e.LeaveRequests)
            .WithOne(lr => lr.Employee)
            .HasForeignKey(lr => lr.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_leave_requests_employee_id");

        builder.HasMany(e => e.RosterEntries)
            .WithOne(re => re.Employee)
            .HasForeignKey(re => re.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_employee_rosters_employee_id");

        builder.HasMany(e => e.Payslips)
            .WithOne(p => p.Employee)
            .HasForeignKey(p => p.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_payslips_employee_id");

        builder.HasMany(e => e.AssetAssignments)
            .WithOne(aa => aa.Employee)
            .HasForeignKey(aa => aa.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_asset_assignments_employee_id");
    }
}

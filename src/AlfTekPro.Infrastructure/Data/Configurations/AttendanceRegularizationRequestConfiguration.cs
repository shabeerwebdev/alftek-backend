using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AlfTekPro.Domain.Entities.Workforce;

namespace AlfTekPro.Infrastructure.Data.Configurations;

public class AttendanceRegularizationRequestConfiguration
    : IEntityTypeConfiguration<AttendanceRegularizationRequest>
{
    public void Configure(EntityTypeBuilder<AttendanceRegularizationRequest> builder)
    {
        builder.ToTable("attendance_regularization_requests");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id");

        builder.Property(r => r.TenantId).IsRequired().HasColumnName("tenant_id");
        builder.Property(r => r.EmployeeId).IsRequired().HasColumnName("employee_id");
        builder.Property(r => r.AttendanceDate).IsRequired().HasColumnName("attendance_date").HasColumnType("date");
        builder.Property(r => r.RequestedStatus).IsRequired().HasColumnName("requested_status")
            .HasConversion<string>().HasMaxLength(20);
        builder.Property(r => r.RequestedClockIn).HasColumnName("requested_clock_in");
        builder.Property(r => r.RequestedClockOut).HasColumnName("requested_clock_out");
        builder.Property(r => r.Reason).IsRequired().HasMaxLength(1000).HasColumnName("reason");
        builder.Property(r => r.Status).IsRequired().HasColumnName("status")
            .HasConversion<string>().HasMaxLength(20).HasDefaultValue(Domain.Enums.RegularizationStatus.Pending);
        builder.Property(r => r.ReviewedBy).HasColumnName("reviewed_by");
        builder.Property(r => r.ReviewedAt).HasColumnName("reviewed_at");
        builder.Property(r => r.ReviewerComments).HasMaxLength(1000).HasColumnName("reviewer_comments");
        builder.Property(r => r.CreatedAt).IsRequired().HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(r => r.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(r => r.TenantId).HasDatabaseName("ix_reg_requests_tenant_id");
        builder.HasIndex(r => r.EmployeeId).HasDatabaseName("ix_reg_requests_employee_id");
        builder.HasIndex(r => new { r.TenantId, r.Status }).HasDatabaseName("ix_reg_requests_tenant_status");

        builder.HasOne(r => r.Reviewer).WithMany().HasForeignKey(r => r.ReviewedBy)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_reg_requests_reviewed_by");
    }
}

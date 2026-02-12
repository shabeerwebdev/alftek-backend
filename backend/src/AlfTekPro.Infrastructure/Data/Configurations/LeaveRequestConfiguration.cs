using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AlfTekPro.Domain.Entities.Leave;

namespace AlfTekPro.Infrastructure.Data.Configurations;

public class LeaveRequestConfiguration : IEntityTypeConfiguration<LeaveRequest>
{
    public void Configure(EntityTypeBuilder<LeaveRequest> builder)
    {
        builder.ToTable("leave_requests");

        builder.HasKey(lr => lr.Id);
        builder.Property(lr => lr.Id).HasColumnName("id");

        builder.Property(lr => lr.TenantId).IsRequired().HasColumnName("tenant_id");
        builder.Property(lr => lr.EmployeeId).IsRequired().HasColumnName("employee_id");
        builder.Property(lr => lr.LeaveTypeId).IsRequired().HasColumnName("leave_type_id");
        builder.Property(lr => lr.StartDate).IsRequired().HasColumnName("start_date");
        builder.Property(lr => lr.EndDate).IsRequired().HasColumnName("end_date");
        builder.Property(lr => lr.DaysCount).IsRequired().HasColumnType("decimal(4,2)").HasColumnName("days_count");
        builder.Property(lr => lr.Reason).HasMaxLength(1000).HasColumnName("reason");
        builder.Property(lr => lr.Status).IsRequired().HasColumnName("status").HasConversion<string>().HasMaxLength(20);
        builder.Property(lr => lr.ApprovedBy).HasColumnName("approved_by");
        builder.Property(lr => lr.ApprovedAt).HasColumnName("approved_at");
        builder.Property(lr => lr.ApproverComments).HasMaxLength(1000).HasColumnName("approver_comments");
        builder.Property(lr => lr.CreatedAt).IsRequired().HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(lr => lr.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(lr => lr.TenantId).HasDatabaseName("ix_leave_requests_tenant_id");
        builder.HasIndex(lr => lr.EmployeeId).HasDatabaseName("ix_leave_requests_employee_id");
        builder.HasIndex(lr => new { lr.TenantId, lr.Status }).HasDatabaseName("ix_leave_requests_tenant_id_status");
        builder.HasIndex(lr => new { lr.EmployeeId, lr.StartDate, lr.EndDate }).HasDatabaseName("ix_leave_requests_employee_id_dates");

        builder.HasOne(lr => lr.Approver).WithMany().HasForeignKey(lr => lr.ApprovedBy)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_leave_requests_approved_by");
    }
}

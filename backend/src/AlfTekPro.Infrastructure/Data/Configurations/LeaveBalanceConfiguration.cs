using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AlfTekPro.Domain.Entities.Leave;

namespace AlfTekPro.Infrastructure.Data.Configurations;

public class LeaveBalanceConfiguration : IEntityTypeConfiguration<LeaveBalance>
{
    public void Configure(EntityTypeBuilder<LeaveBalance> builder)
    {
        builder.ToTable("leave_balances");

        builder.HasKey(lb => lb.Id);
        builder.Property(lb => lb.Id).HasColumnName("id");

        builder.Property(lb => lb.TenantId).IsRequired().HasColumnName("tenant_id");
        builder.Property(lb => lb.EmployeeId).IsRequired().HasColumnName("employee_id");
        builder.Property(lb => lb.LeaveTypeId).IsRequired().HasColumnName("leave_type_id");
        builder.Property(lb => lb.Year).IsRequired().HasColumnName("year");
        builder.Property(lb => lb.Accrued).IsRequired().HasColumnType("decimal(5,2)").HasColumnName("accrued").HasDefaultValue(0);
        builder.Property(lb => lb.Used).IsRequired().HasColumnType("decimal(5,2)").HasColumnName("used").HasDefaultValue(0);
        builder.Property(lb => lb.CreatedAt).IsRequired().HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(lb => lb.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(lb => lb.TenantId).HasDatabaseName("ix_leave_balances_tenant_id");
        builder.HasIndex(lb => lb.EmployeeId).HasDatabaseName("ix_leave_balances_employee_id");
        builder.HasIndex(lb => new { lb.EmployeeId, lb.LeaveTypeId, lb.Year }).IsUnique()
            .HasDatabaseName("ix_leave_balances_employee_id_leave_type_id_year");
    }
}

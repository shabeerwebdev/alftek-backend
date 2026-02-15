using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AlfTekPro.Domain.Entities.Leave;

namespace AlfTekPro.Infrastructure.Data.Configurations;

public class LeaveTypeConfiguration : IEntityTypeConfiguration<LeaveType>
{
    public void Configure(EntityTypeBuilder<LeaveType> builder)
    {
        builder.ToTable("leave_types");

        builder.HasKey(lt => lt.Id);
        builder.Property(lt => lt.Id).HasColumnName("id");

        builder.Property(lt => lt.TenantId).IsRequired().HasColumnName("tenant_id");
        builder.Property(lt => lt.Name).IsRequired().HasMaxLength(200).HasColumnName("name");
        builder.Property(lt => lt.Code).IsRequired().HasMaxLength(50).HasColumnName("code");
        builder.Property(lt => lt.MaxDaysPerYear).IsRequired().HasColumnType("decimal(5,2)").HasColumnName("max_days_per_year");
        builder.Property(lt => lt.IsCarryForward).IsRequired().HasColumnName("is_carry_forward").HasDefaultValue(false);
        builder.Property(lt => lt.RequiresApproval).IsRequired().HasColumnName("requires_approval").HasDefaultValue(true);
        builder.Property(lt => lt.IsActive).IsRequired().HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(lt => lt.CreatedAt).IsRequired().HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(lt => lt.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(lt => lt.TenantId).HasDatabaseName("ix_leave_types_tenant_id");
        builder.HasIndex(lt => new { lt.TenantId, lt.Code }).IsUnique().HasDatabaseName("ix_leave_types_tenant_id_code");

        builder.HasMany(lt => lt.LeaveBalances).WithOne(lb => lb.LeaveType).HasForeignKey(lb => lb.LeaveTypeId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_leave_balances_leave_type_id");
        builder.HasMany(lt => lt.LeaveRequests).WithOne(lr => lr.LeaveType).HasForeignKey(lr => lr.LeaveTypeId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_leave_requests_leave_type_id");
    }
}

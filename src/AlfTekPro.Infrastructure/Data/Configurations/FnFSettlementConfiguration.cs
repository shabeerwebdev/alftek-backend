using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AlfTekPro.Domain.Entities.Payroll;

namespace AlfTekPro.Infrastructure.Data.Configurations;

public class FnFSettlementConfiguration : IEntityTypeConfiguration<FnFSettlement>
{
    public void Configure(EntityTypeBuilder<FnFSettlement> builder)
    {
        builder.ToTable("fnf_settlements");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id");

        builder.Property(s => s.TenantId).IsRequired().HasColumnName("tenant_id");
        builder.Property(s => s.EmployeeId).IsRequired().HasColumnName("employee_id");
        builder.Property(s => s.LastWorkingDay).IsRequired().HasColumnName("last_working_day").HasColumnType("date");
        builder.Property(s => s.Status).IsRequired().HasConversion<string>().HasMaxLength(20).HasColumnName("status");

        foreach (var (prop, col) in new[]
        {
            ("UnpaidSalary", "unpaid_salary"),
            ("GratuityAmount", "gratuity_amount"),
            ("UnusedLeaveEncashment", "unused_leave_encashment"),
            ("OtherEarnings", "other_earnings"),
            ("LoanDeductions", "loan_deductions"),
            ("TaxDeductions", "tax_deductions"),
            ("OtherDeductions", "other_deductions"),
            ("TotalEarnings", "total_earnings"),
            ("TotalDeductions", "total_deductions"),
            ("NetSettlementAmount", "net_settlement_amount")
        })
        {
            builder.Property(prop).HasColumnName(col).HasColumnType("decimal(14,2)").HasDefaultValue(0m);
        }

        builder.Property(s => s.Notes).HasMaxLength(2000).HasColumnName("notes");
        builder.Property(s => s.ApprovedBy).HasColumnName("approved_by");
        builder.Property(s => s.ApprovedAt).HasColumnName("approved_at");
        builder.Property(s => s.PaidAt).HasColumnName("paid_at");
        builder.Property(s => s.CreatedAt).IsRequired().HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(s => s.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(s => s.TenantId).HasDatabaseName("ix_fnf_settlements_tenant_id");
        builder.HasIndex(s => s.EmployeeId).IsUnique().HasDatabaseName("ix_fnf_settlements_employee_id");

        builder.HasOne(s => s.Approver).WithMany().HasForeignKey(s => s.ApprovedBy)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_fnf_settlements_approved_by");
    }
}

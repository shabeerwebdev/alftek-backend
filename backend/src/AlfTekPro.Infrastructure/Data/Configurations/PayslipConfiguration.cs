using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AlfTekPro.Domain.Entities.Payroll;

namespace AlfTekPro.Infrastructure.Data.Configurations;

public class PayslipConfiguration : IEntityTypeConfiguration<Payslip>
{
    public void Configure(EntityTypeBuilder<Payslip> builder)
    {
        builder.ToTable("payslips");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id");

        builder.Property(p => p.TenantId).IsRequired().HasColumnName("tenant_id");
        builder.Property(p => p.PayrollRunId).IsRequired().HasColumnName("payroll_run_id");
        builder.Property(p => p.EmployeeId).IsRequired().HasColumnName("employee_id");
        builder.Property(p => p.WorkingDays).IsRequired().HasColumnName("working_days");
        builder.Property(p => p.PresentDays).IsRequired().HasColumnName("present_days");
        builder.Property(p => p.GrossEarnings).IsRequired().HasColumnType("decimal(12,2)").HasColumnName("gross_earnings");
        builder.Property(p => p.TotalDeductions).IsRequired().HasColumnType("decimal(12,2)").HasColumnName("total_deductions");
        builder.Property(p => p.NetPay).IsRequired().HasColumnType("decimal(12,2)").HasColumnName("net_pay");
        builder.Property(p => p.BreakdownJson).IsRequired().HasColumnType("jsonb").HasColumnName("breakdown_json");
        builder.Property(p => p.PdfPath).HasMaxLength(500).HasColumnName("pdf_path");
        builder.Property(p => p.CreatedAt).IsRequired().HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(p => p.TenantId).HasDatabaseName("ix_payslips_tenant_id");
        builder.HasIndex(p => p.EmployeeId).HasDatabaseName("ix_payslips_employee_id");
        builder.HasIndex(p => new { p.PayrollRunId, p.EmployeeId }).IsUnique().HasDatabaseName("ix_payslips_payroll_run_id_employee_id");
        builder.HasIndex(p => p.BreakdownJson).HasMethod("gin").HasDatabaseName("ix_payslips_breakdown_json_gin");
    }
}

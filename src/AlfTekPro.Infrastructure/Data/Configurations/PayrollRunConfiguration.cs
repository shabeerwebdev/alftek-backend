using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AlfTekPro.Domain.Entities.Payroll;
using AlfTekPro.Domain.Enums;

namespace AlfTekPro.Infrastructure.Data.Configurations;

public class PayrollRunConfiguration : IEntityTypeConfiguration<PayrollRun>
{
    public void Configure(EntityTypeBuilder<PayrollRun> builder)
    {
        builder.ToTable("payroll_runs");

        builder.HasKey(pr => pr.Id);
        builder.Property(pr => pr.Id).HasColumnName("id");

        builder.Property(pr => pr.TenantId).IsRequired().HasColumnName("tenant_id");
        builder.Property(pr => pr.Month).IsRequired().HasColumnName("month");
        builder.Property(pr => pr.Year).IsRequired().HasColumnName("year");
        builder.Property(pr => pr.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50)
            .HasColumnName("status")
            .HasDefaultValue(PayrollRunStatus.Draft);
        builder.Property(pr => pr.S3PathPdfBundle).HasMaxLength(500).HasColumnName("s3_path_pdf_bundle");
        builder.Property(pr => pr.ProcessedAt).HasColumnName("processed_at");
        builder.Property(pr => pr.CreatedAt).IsRequired().HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(pr => pr.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(pr => pr.TenantId).HasDatabaseName("ix_payroll_runs_tenant_id");
        builder.HasIndex(pr => new { pr.TenantId, pr.Month, pr.Year }).IsUnique().HasDatabaseName("ix_payroll_runs_tenant_id_month_year");

        builder.HasMany(pr => pr.Payslips).WithOne(p => p.PayrollRun).HasForeignKey(p => p.PayrollRunId)
            .OnDelete(DeleteBehavior.Cascade).HasConstraintName("fk_payslips_payroll_run_id");
    }
}

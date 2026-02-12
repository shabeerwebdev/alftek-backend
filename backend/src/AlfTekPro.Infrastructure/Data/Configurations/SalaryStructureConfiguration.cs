using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AlfTekPro.Domain.Entities.Payroll;

namespace AlfTekPro.Infrastructure.Data.Configurations;

public class SalaryStructureConfiguration : IEntityTypeConfiguration<SalaryStructure>
{
    public void Configure(EntityTypeBuilder<SalaryStructure> builder)
    {
        builder.ToTable("salary_structures");

        builder.HasKey(ss => ss.Id);
        builder.Property(ss => ss.Id).HasColumnName("id");

        builder.Property(ss => ss.TenantId).IsRequired().HasColumnName("tenant_id");
        builder.Property(ss => ss.Name).IsRequired().HasMaxLength(200).HasColumnName("name");
        builder.Property(ss => ss.ComponentsJson).IsRequired().HasColumnType("jsonb").HasColumnName("components_json");
        builder.Property(ss => ss.CreatedAt).IsRequired().HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(ss => ss.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(ss => ss.TenantId).HasDatabaseName("ix_salary_structures_tenant_id");
        builder.HasIndex(ss => new { ss.TenantId, ss.Name }).HasDatabaseName("ix_salary_structures_tenant_id_name");
        builder.HasIndex(ss => ss.ComponentsJson).HasMethod("gin").HasDatabaseName("ix_salary_structures_components_json_gin");

        builder.HasMany(ss => ss.EmployeeJobHistories).WithOne(jh => jh.SalaryTier).HasForeignKey(jh => jh.SalaryTierId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_employee_job_histories_salary_tier_id");
    }
}

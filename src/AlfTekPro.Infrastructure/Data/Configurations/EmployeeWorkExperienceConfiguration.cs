using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AlfTekPro.Domain.Entities.CoreHR;

namespace AlfTekPro.Infrastructure.Data.Configurations;

public class EmployeeWorkExperienceConfiguration : IEntityTypeConfiguration<EmployeeWorkExperience>
{
    public void Configure(EntityTypeBuilder<EmployeeWorkExperience> builder)
    {
        builder.ToTable("employee_work_experiences");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.TenantId).IsRequired().HasColumnName("tenant_id");
        builder.Property(e => e.EmployeeId).IsRequired().HasColumnName("employee_id");
        builder.Property(e => e.CompanyName).IsRequired().HasMaxLength(200).HasColumnName("company_name");
        builder.Property(e => e.Designation).HasMaxLength(150).HasColumnName("designation");
        builder.Property(e => e.FromDate).IsRequired().HasColumnName("from_date");
        builder.Property(e => e.ToDate).HasColumnName("to_date");
        builder.Property(e => e.IsCurrent).IsRequired().HasDefaultValue(false).HasColumnName("is_current");
        builder.Property(e => e.Responsibilities).HasMaxLength(2000).HasColumnName("responsibilities");
        builder.Property(e => e.ReasonForLeaving).HasMaxLength(500).HasColumnName("reason_for_leaving");
        builder.Property(e => e.CreatedAt).IsRequired().HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.HasIndex(e => e.EmployeeId).HasDatabaseName("ix_employee_work_experiences_employee_id");
        builder.HasIndex(e => e.TenantId).HasDatabaseName("ix_employee_work_experiences_tenant_id");
    }
}

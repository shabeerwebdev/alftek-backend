using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AlfTekPro.Domain.Entities.CoreHR;

namespace AlfTekPro.Infrastructure.Data.Configurations;

public class EmployeeQualificationConfiguration : IEntityTypeConfiguration<EmployeeQualification>
{
    public void Configure(EntityTypeBuilder<EmployeeQualification> builder)
    {
        builder.ToTable("employee_qualifications");
        builder.HasKey(q => q.Id);
        builder.Property(q => q.Id).HasColumnName("id");
        builder.Property(q => q.TenantId).IsRequired().HasColumnName("tenant_id");
        builder.Property(q => q.EmployeeId).IsRequired().HasColumnName("employee_id");
        builder.Property(q => q.Degree).IsRequired().HasMaxLength(200).HasColumnName("degree");
        builder.Property(q => q.FieldOfStudy).HasMaxLength(200).HasColumnName("field_of_study");
        builder.Property(q => q.Institution).HasMaxLength(200).HasColumnName("institution");
        builder.Property(q => q.PassingYear).HasColumnName("passing_year");
        builder.Property(q => q.Grade).HasMaxLength(50).HasColumnName("grade");
        builder.Property(q => q.Notes).HasMaxLength(1000).HasColumnName("notes");
        builder.Property(q => q.CreatedAt).IsRequired().HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(q => q.UpdatedAt).HasColumnName("updated_at");
        builder.HasIndex(q => q.EmployeeId).HasDatabaseName("ix_employee_qualifications_employee_id");
        builder.HasIndex(q => q.TenantId).HasDatabaseName("ix_employee_qualifications_tenant_id");
    }
}

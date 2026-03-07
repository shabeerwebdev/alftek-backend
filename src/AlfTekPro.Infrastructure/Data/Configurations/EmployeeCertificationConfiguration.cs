using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AlfTekPro.Domain.Entities.CoreHR;

namespace AlfTekPro.Infrastructure.Data.Configurations;

public class EmployeeCertificationConfiguration : IEntityTypeConfiguration<EmployeeCertification>
{
    public void Configure(EntityTypeBuilder<EmployeeCertification> builder)
    {
        builder.ToTable("employee_certifications");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id");
        builder.Property(c => c.TenantId).IsRequired().HasColumnName("tenant_id");
        builder.Property(c => c.EmployeeId).IsRequired().HasColumnName("employee_id");
        builder.Property(c => c.CertificationName).IsRequired().HasMaxLength(200).HasColumnName("certification_name");
        builder.Property(c => c.IssuingOrganization).HasMaxLength(200).HasColumnName("issuing_organization");
        builder.Property(c => c.CertificateNumber).HasMaxLength(100).HasColumnName("certificate_number");
        builder.Property(c => c.IssueDate).HasColumnName("issue_date");
        builder.Property(c => c.ExpiryDate).HasColumnName("expiry_date");
        builder.Property(c => c.HasExpiry).IsRequired().HasDefaultValue(false).HasColumnName("has_expiry");
        builder.Property(c => c.Notes).HasMaxLength(1000).HasColumnName("notes");
        builder.Property(c => c.CreatedAt).IsRequired().HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at");
        builder.HasIndex(c => c.EmployeeId).HasDatabaseName("ix_employee_certifications_employee_id");
        builder.HasIndex(c => c.TenantId).HasDatabaseName("ix_employee_certifications_tenant_id");
    }
}

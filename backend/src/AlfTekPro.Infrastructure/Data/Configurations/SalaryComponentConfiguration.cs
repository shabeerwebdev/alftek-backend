using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AlfTekPro.Domain.Entities.Payroll;

namespace AlfTekPro.Infrastructure.Data.Configurations;

public class SalaryComponentConfiguration : IEntityTypeConfiguration<SalaryComponent>
{
    public void Configure(EntityTypeBuilder<SalaryComponent> builder)
    {
        builder.ToTable("salary_components");

        builder.HasKey(sc => sc.Id);
        builder.Property(sc => sc.Id).HasColumnName("id");

        builder.Property(sc => sc.TenantId)
            .IsRequired()
            .HasColumnName("tenant_id");

        builder.Property(sc => sc.Name)
            .IsRequired()
            .HasMaxLength(200)
            .HasColumnName("name");

        builder.Property(sc => sc.Code)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("code");

        builder.Property(sc => sc.Type)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50)
            .HasColumnName("type");

        builder.Property(sc => sc.IsTaxable)
            .IsRequired()
            .HasColumnName("is_taxable")
            .HasDefaultValue(false);

        builder.Property(sc => sc.IsActive)
            .IsRequired()
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(sc => sc.CreatedAt)
            .IsRequired()
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(sc => sc.UpdatedAt)
            .HasColumnName("updated_at");

        builder.HasIndex(sc => sc.TenantId).HasDatabaseName("ix_salary_components_tenant_id");
        builder.HasIndex(sc => new { sc.TenantId, sc.Code }).IsUnique().HasDatabaseName("ix_salary_components_tenant_id_code");
    }
}

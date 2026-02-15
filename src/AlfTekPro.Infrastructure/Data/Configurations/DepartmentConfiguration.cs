using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AlfTekPro.Domain.Entities.CoreHR;

namespace AlfTekPro.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for Department entity
/// Supports hierarchical self-referencing structure
/// </summary>
public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        // Table name
        builder.ToTable("departments");

        // Primary key
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id)
            .HasColumnName("id");

        // Tenant isolation
        builder.Property(d => d.TenantId)
            .IsRequired()
            .HasColumnName("tenant_id");

        // Properties
        builder.Property(d => d.Name)
            .IsRequired()
            .HasMaxLength(200)
            .HasColumnName("name");

        builder.Property(d => d.Code)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("code");

        builder.Property(d => d.Description)
            .HasMaxLength(500)
            .HasColumnName("description");

        builder.Property(d => d.IsActive)
            .IsRequired()
            .HasDefaultValue(true)
            .HasColumnName("is_active");

        builder.Property(d => d.HeadUserId)
            .HasColumnName("head_user_id");

        builder.Property(d => d.ParentDepartmentId)
            .HasColumnName("parent_department_id");

        builder.Property(d => d.CreatedAt)
            .IsRequired()
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(d => d.UpdatedAt)
            .HasColumnName("updated_at");

        // Indexes
        builder.HasIndex(d => d.TenantId)
            .HasDatabaseName("ix_departments_tenant_id");

        builder.HasIndex(d => d.ParentDepartmentId)
            .HasDatabaseName("ix_departments_parent_department_id");

        builder.HasIndex(d => new { d.TenantId, d.Name })
            .HasDatabaseName("ix_departments_tenant_id_name");

        // Self-referencing relationship (hierarchy)
        builder.HasOne(d => d.ParentDepartment)
            .WithMany(d => d.ChildDepartments)
            .HasForeignKey(d => d.ParentDepartmentId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_departments_parent_department_id");

        // Navigation properties (other relationships configured in their respective configs)
    }
}

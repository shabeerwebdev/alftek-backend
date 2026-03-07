using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AlfTekPro.Domain.Entities.CoreHR;

namespace AlfTekPro.Infrastructure.Data.Configurations;

public class EmployeeDocumentConfiguration : IEntityTypeConfiguration<EmployeeDocument>
{
    public void Configure(EntityTypeBuilder<EmployeeDocument> builder)
    {
        builder.ToTable("employee_documents");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).HasColumnName("id");
        builder.Property(d => d.TenantId).IsRequired().HasColumnName("tenant_id");
        builder.Property(d => d.EmployeeId).IsRequired().HasColumnName("employee_id");
        builder.Property(d => d.DocumentType).IsRequired().HasMaxLength(50).HasColumnName("document_type");
        builder.Property(d => d.FileName).IsRequired().HasMaxLength(255).HasColumnName("file_name");
        builder.Property(d => d.StorageKey).IsRequired().HasMaxLength(500).HasColumnName("storage_key");
        builder.Property(d => d.ContentType).IsRequired().HasMaxLength(100).HasColumnName("content_type");
        builder.Property(d => d.FileSizeBytes).IsRequired().HasColumnName("file_size_bytes");
        builder.Property(d => d.UploadedById).HasColumnName("uploaded_by_id");
        builder.Property(d => d.Notes).HasMaxLength(1000).HasColumnName("notes");
        builder.Property(d => d.CreatedAt).IsRequired().HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(d => d.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(d => d.TenantId).HasDatabaseName("ix_employee_documents_tenant_id");
        builder.HasIndex(d => d.EmployeeId).HasDatabaseName("ix_employee_documents_employee_id");
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AlfTekPro.Domain.Entities.Platform;

namespace AlfTekPro.Infrastructure.Data.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id");
        builder.Property(a => a.TenantId).HasColumnName("tenant_id");
        builder.Property(a => a.UserId).HasColumnName("user_id");
        builder.Property(a => a.UserEmail).HasColumnName("user_email").HasMaxLength(255);
        builder.Property(a => a.Action).IsRequired().HasColumnName("action").HasMaxLength(20);
        builder.Property(a => a.EntityName).IsRequired().HasColumnName("entity_name").HasMaxLength(100);
        builder.Property(a => a.EntityId).IsRequired().HasColumnName("entity_id").HasMaxLength(100);
        builder.Property(a => a.OldValues).HasColumnName("old_values").HasColumnType("jsonb");
        builder.Property(a => a.NewValues).HasColumnName("new_values").HasColumnType("jsonb");
        builder.Property(a => a.IpAddress).HasColumnName("ip_address").HasMaxLength(45);
        builder.Property(a => a.CreatedAt).IsRequired().HasColumnName("created_at");

        builder.HasIndex(a => a.TenantId).HasDatabaseName("ix_audit_logs_tenant_id");
        builder.HasIndex(a => a.EntityName).HasDatabaseName("ix_audit_logs_entity_name");
        builder.HasIndex(a => a.CreatedAt).HasDatabaseName("ix_audit_logs_created_at");
        builder.HasIndex(a => a.UserId).HasDatabaseName("ix_audit_logs_user_id");
    }
}

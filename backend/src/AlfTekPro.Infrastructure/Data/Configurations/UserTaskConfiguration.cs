using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AlfTekPro.Domain.Entities.Workflow;

namespace AlfTekPro.Infrastructure.Data.Configurations;

public class UserTaskConfiguration : IEntityTypeConfiguration<UserTask>
{
    public void Configure(EntityTypeBuilder<UserTask> builder)
    {
        builder.ToTable("user_tasks");

        builder.HasKey(ut => ut.Id);
        builder.Property(ut => ut.Id).HasColumnName("id");

        builder.Property(ut => ut.TenantId).IsRequired().HasColumnName("tenant_id");
        builder.Property(ut => ut.OwnerUserId).IsRequired().HasColumnName("owner_user_id");
        builder.Property(ut => ut.Title).IsRequired().HasMaxLength(500).HasColumnName("title");
        builder.Property(ut => ut.EntityType).IsRequired().HasMaxLength(100).HasColumnName("entity_type");
        builder.Property(ut => ut.EntityId).IsRequired().HasColumnName("entity_id");
        builder.Property(ut => ut.Status).IsRequired().HasMaxLength(50).HasColumnName("status").HasDefaultValue("Pending");
        builder.Property(ut => ut.ActionUrl).HasMaxLength(500).HasColumnName("action_url");
        builder.Property(ut => ut.Priority).IsRequired().HasMaxLength(20).HasColumnName("priority").HasDefaultValue("Normal");
        builder.Property(ut => ut.DueDate).HasColumnName("due_date");
        builder.Property(ut => ut.ActionedAt).HasColumnName("actioned_at");
        builder.Property(ut => ut.CreatedAt).IsRequired().HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(ut => ut.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(ut => ut.TenantId).HasDatabaseName("ix_user_tasks_tenant_id");
        builder.HasIndex(ut => ut.OwnerUserId).HasDatabaseName("ix_user_tasks_owner_user_id");
        builder.HasIndex(ut => new { ut.OwnerUserId, ut.Status }).HasDatabaseName("ix_user_tasks_owner_user_id_status");
        builder.HasIndex(ut => new { ut.TenantId, ut.EntityType, ut.EntityId }).HasDatabaseName("ix_user_tasks_tenant_id_entity");

        builder.HasOne(ut => ut.Owner).WithMany().HasForeignKey(ut => ut.OwnerUserId)
            .OnDelete(DeleteBehavior.Cascade).HasConstraintName("fk_user_tasks_owner_user_id");
    }
}

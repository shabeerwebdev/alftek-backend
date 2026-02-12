using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AlfTekPro.Domain.Entities.Assets;

namespace AlfTekPro.Infrastructure.Data.Configurations;

public class AssetAssignmentConfiguration : IEntityTypeConfiguration<AssetAssignment>
{
    public void Configure(EntityTypeBuilder<AssetAssignment> builder)
    {
        builder.ToTable("asset_assignments");

        builder.HasKey(aa => aa.Id);
        builder.Property(aa => aa.Id).HasColumnName("id");

        builder.Property(aa => aa.TenantId).IsRequired().HasColumnName("tenant_id");
        builder.Property(aa => aa.AssetId).IsRequired().HasColumnName("asset_id");
        builder.Property(aa => aa.EmployeeId).IsRequired().HasColumnName("employee_id");
        builder.Property(aa => aa.AssignedDate).IsRequired().HasColumnName("assigned_date");
        builder.Property(aa => aa.ReturnedDate).HasColumnName("returned_date");
        builder.Property(aa => aa.AssignedCondition).HasMaxLength(200).HasColumnName("assigned_condition");
        builder.Property(aa => aa.ReturnedCondition).HasMaxLength(200).HasColumnName("returned_condition");
        builder.Property(aa => aa.ReturnNotes).HasMaxLength(1000).HasColumnName("return_notes");
        builder.Property(aa => aa.CreatedAt).IsRequired().HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(aa => aa.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(aa => aa.TenantId).HasDatabaseName("ix_asset_assignments_tenant_id");
        builder.HasIndex(aa => aa.AssetId).HasDatabaseName("ix_asset_assignments_asset_id");
        builder.HasIndex(aa => aa.EmployeeId).HasDatabaseName("ix_asset_assignments_employee_id");
        builder.HasIndex(aa => new { aa.AssetId, aa.ReturnedDate }).HasDatabaseName("ix_asset_assignments_asset_id_returned_date");
    }
}

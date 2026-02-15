using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AlfTekPro.Domain.Entities.Assets;

namespace AlfTekPro.Infrastructure.Data.Configurations;

public class AssetConfiguration : IEntityTypeConfiguration<Asset>
{
    public void Configure(EntityTypeBuilder<Asset> builder)
    {
        builder.ToTable("assets");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id");

        builder.Property(a => a.TenantId).IsRequired().HasColumnName("tenant_id");
        builder.Property(a => a.AssetCode).IsRequired().HasMaxLength(100).HasColumnName("asset_code");
        builder.Property(a => a.AssetType).IsRequired().HasMaxLength(100).HasColumnName("asset_type");
        builder.Property(a => a.Make).HasMaxLength(100).HasColumnName("make");
        builder.Property(a => a.Model).HasMaxLength(100).HasColumnName("model");
        builder.Property(a => a.SerialNumber).HasMaxLength(200).HasColumnName("serial_number");
        builder.Property(a => a.PurchaseDate).HasColumnName("purchase_date");
        builder.Property(a => a.PurchasePrice).HasColumnType("decimal(12,2)").HasColumnName("purchase_price");
        builder.Property(a => a.Status).IsRequired().HasMaxLength(50).HasColumnName("status").HasDefaultValue("Available");
        builder.Property(a => a.CreatedAt).IsRequired().HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(a => a.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(a => a.TenantId).HasDatabaseName("ix_assets_tenant_id");
        builder.HasIndex(a => new { a.TenantId, a.AssetCode }).IsUnique().HasDatabaseName("ix_assets_tenant_id_asset_code");
        builder.HasIndex(a => new { a.TenantId, a.Status }).HasDatabaseName("ix_assets_tenant_id_status");

        builder.HasMany(a => a.Assignments).WithOne(aa => aa.Asset).HasForeignKey(aa => aa.AssetId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_asset_assignments_asset_id");
    }
}

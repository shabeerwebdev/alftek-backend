using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AlfTekPro.Domain.Entities.Platform;

namespace AlfTekPro.Infrastructure.Data.Configurations;

public class StatutoryContributionRuleConfiguration
    : IEntityTypeConfiguration<StatutoryContributionRule>
{
    public void Configure(EntityTypeBuilder<StatutoryContributionRule> builder)
    {
        builder.ToTable("statutory_contribution_rules");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id");

        builder.Property(r => r.RegionId).IsRequired().HasColumnName("region_id");
        builder.Property(r => r.Code).IsRequired().HasMaxLength(50).HasColumnName("code");
        builder.Property(r => r.Name).IsRequired().HasMaxLength(200).HasColumnName("name");
        builder.Property(r => r.Party).IsRequired().HasConversion<string>().HasMaxLength(20)
            .HasColumnName("party");
        builder.Property(r => r.ComponentType).IsRequired().HasConversion<string>().HasMaxLength(20)
            .HasColumnName("component_type");
        builder.Property(r => r.CalculationType).IsRequired().HasMaxLength(20)
            .HasColumnName("calculation_type");
        builder.Property(r => r.Rate).IsRequired().HasColumnType("decimal(8,4)").HasColumnName("rate");
        builder.Property(r => r.MaxContributionBase).HasColumnType("decimal(12,2)")
            .HasColumnName("max_contribution_base");
        builder.Property(r => r.MaxContributionAmount).HasColumnType("decimal(12,2)")
            .HasColumnName("max_contribution_amount");
        builder.Property(r => r.IsActive).IsRequired().HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(r => r.EffectiveFrom).IsRequired().HasColumnName("effective_from")
            .HasColumnType("date");
        builder.Property(r => r.EffectiveTo).HasColumnName("effective_to").HasColumnType("date");
        builder.Property(r => r.CreatedAt).IsRequired().HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(r => r.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(r => r.RegionId).HasDatabaseName("ix_statutory_rules_region_id");
        builder.HasIndex(r => new { r.RegionId, r.Code }).IsUnique()
            .HasDatabaseName("ix_statutory_rules_region_code");

        builder.HasOne(r => r.Region).WithMany().HasForeignKey(r => r.RegionId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_statutory_rules_region_id");
    }
}

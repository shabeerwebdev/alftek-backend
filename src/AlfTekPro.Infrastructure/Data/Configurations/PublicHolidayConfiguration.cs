using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AlfTekPro.Domain.Entities.CoreHR;

namespace AlfTekPro.Infrastructure.Data.Configurations;

public class PublicHolidayConfiguration : IEntityTypeConfiguration<PublicHoliday>
{
    public void Configure(EntityTypeBuilder<PublicHoliday> builder)
    {
        builder.ToTable("public_holidays");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id");
        builder.Property(p => p.TenantId).IsRequired().HasColumnName("tenant_id");
        builder.Property(p => p.Date).IsRequired().HasColumnName("date").HasColumnType("date");
        builder.Property(p => p.Name).IsRequired().HasMaxLength(200).HasColumnName("name");
        builder.Property(p => p.IsRecurring).IsRequired().HasDefaultValue(false).HasColumnName("is_recurring");
        builder.Property(p => p.Description).HasMaxLength(500).HasColumnName("description");
        builder.Property(p => p.CreatedAt).IsRequired().HasColumnName("created_at");
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(p => new { p.TenantId, p.Date }).HasDatabaseName("ix_public_holidays_tenant_id_date");
    }
}

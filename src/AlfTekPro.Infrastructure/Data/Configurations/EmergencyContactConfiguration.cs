using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AlfTekPro.Domain.Entities.CoreHR;

namespace AlfTekPro.Infrastructure.Data.Configurations;

public class EmergencyContactConfiguration : IEntityTypeConfiguration<EmergencyContact>
{
    public void Configure(EntityTypeBuilder<EmergencyContact> builder)
    {
        builder.ToTable("emergency_contacts");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.TenantId).IsRequired().HasColumnName("tenant_id");
        builder.Property(e => e.EmployeeId).IsRequired().HasColumnName("employee_id");
        builder.Property(e => e.Name).IsRequired().HasMaxLength(200).HasColumnName("name");
        builder.Property(e => e.Relationship).IsRequired().HasMaxLength(100).HasColumnName("relationship");
        builder.Property(e => e.PhoneNumber).IsRequired().HasMaxLength(30).HasColumnName("phone_number");
        builder.Property(e => e.AlternatePhone).HasMaxLength(30).HasColumnName("alternate_phone");
        builder.Property(e => e.Email).HasMaxLength(255).HasColumnName("email");
        builder.Property(e => e.Address).HasMaxLength(500).HasColumnName("address");
        builder.Property(e => e.IsPrimary).IsRequired().HasDefaultValue(false).HasColumnName("is_primary");
        builder.Property(e => e.CreatedAt).IsRequired().HasColumnName("created_at");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne(e => e.Employee)
            .WithMany()
            .HasForeignKey(e => e.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.EmployeeId).HasDatabaseName("ix_emergency_contacts_employee_id");
    }
}

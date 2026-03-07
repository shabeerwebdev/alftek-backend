using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AlfTekPro.Domain.Entities.CoreHR;

namespace AlfTekPro.Infrastructure.Data.Configurations;

public class EmployeeBankAccountConfiguration : IEntityTypeConfiguration<EmployeeBankAccount>
{
    public void Configure(EntityTypeBuilder<EmployeeBankAccount> builder)
    {
        builder.ToTable("employee_bank_accounts");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).HasColumnName("id");
        builder.Property(b => b.TenantId).IsRequired().HasColumnName("tenant_id");
        builder.Property(b => b.EmployeeId).IsRequired().HasColumnName("employee_id");
        builder.Property(b => b.BankName).IsRequired().HasMaxLength(200).HasColumnName("bank_name");
        builder.Property(b => b.AccountHolderName).IsRequired().HasMaxLength(200).HasColumnName("account_holder_name");
        builder.Property(b => b.AccountNumber).IsRequired().HasMaxLength(50).HasColumnName("account_number");
        builder.Property(b => b.BranchCode).HasMaxLength(50).HasColumnName("branch_code");
        builder.Property(b => b.SwiftCode).HasMaxLength(20).HasColumnName("swift_code");
        builder.Property(b => b.IbanNumber).HasMaxLength(50).HasColumnName("iban_number");
        builder.Property(b => b.BankCountry).HasMaxLength(5).HasColumnName("bank_country");
        builder.Property(b => b.IsPrimary).IsRequired().HasDefaultValue(false).HasColumnName("is_primary");
        builder.Property(b => b.CreatedAt).IsRequired().HasColumnName("created_at");
        builder.Property(b => b.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne(b => b.Employee)
            .WithMany()
            .HasForeignKey(b => b.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(b => b.EmployeeId).HasDatabaseName("ix_employee_bank_accounts_employee_id");
        builder.HasIndex(b => new { b.EmployeeId, b.IsPrimary }).HasDatabaseName("ix_employee_bank_accounts_employee_id_is_primary");
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AlfTekPro.Domain.Entities.Platform;

namespace AlfTekPro.Infrastructure.Data.Configurations;

public class TenantBankAccountConfiguration : IEntityTypeConfiguration<TenantBankAccount>
{
    public void Configure(EntityTypeBuilder<TenantBankAccount> builder)
    {
        builder.ToTable("tenant_bank_accounts");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id");
        builder.Property(t => t.TenantId).IsRequired().HasColumnName("tenant_id");
        builder.Property(t => t.BankName).IsRequired().HasMaxLength(200).HasColumnName("bank_name");
        builder.Property(t => t.AccountHolderName).IsRequired().HasMaxLength(200).HasColumnName("account_holder_name");
        builder.Property(t => t.AccountNumber).IsRequired().HasMaxLength(100).HasColumnName("account_number");
        builder.Property(t => t.BranchCode).HasMaxLength(50).HasColumnName("branch_code");
        builder.Property(t => t.SwiftCode).HasMaxLength(20).HasColumnName("swift_code");
        builder.Property(t => t.IbanNumber).HasMaxLength(50).HasColumnName("iban_number");
        builder.Property(t => t.BankCountry).HasMaxLength(2).HasColumnName("bank_country");
        builder.Property(t => t.IsPrimary).IsRequired().HasDefaultValue(false).HasColumnName("is_primary");
        builder.Property(t => t.Label).HasMaxLength(100).HasColumnName("label");
        builder.Property(t => t.CreatedAt).IsRequired().HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(t => t.UpdatedAt).HasColumnName("updated_at");
        builder.HasIndex(t => t.TenantId).HasDatabaseName("ix_tenant_bank_accounts_tenant_id");
    }
}

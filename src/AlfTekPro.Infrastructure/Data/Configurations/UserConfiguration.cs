using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AlfTekPro.Domain.Entities.Platform;
using AlfTekPro.Domain.Enums;

namespace AlfTekPro.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for User entity
/// Supports both platform users (SuperAdmin) and tenant users
/// </summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // Table name
        builder.ToTable("users");

        // Primary key
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id)
            .HasColumnName("id");

        // Properties
        builder.Property(u => u.TenantId)
            .HasColumnName("tenant_id");

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(255)
            .HasColumnName("email");

        builder.Property(u => u.FirstName)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnName("first_name");

        builder.Property(u => u.LastName)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnName("last_name");

        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(500)
            .HasColumnName("password_hash");

        builder.Property(u => u.Role)
            .IsRequired()
            .HasColumnName("role")
            .HasConversion<string>()
            .HasMaxLength(10);

        builder.Property(u => u.IsActive)
            .IsRequired()
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(u => u.LastLogin)
            .HasColumnName("last_login");

        builder.Property(u => u.CreatedAt)
            .IsRequired()
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(u => u.UpdatedAt)
            .HasColumnName("updated_at");

        // Indexes
        // Unique email per tenant (composite unique index)
        builder.HasIndex(u => new { u.TenantId, u.Email })
            .IsUnique()
            .HasDatabaseName("ix_users_tenant_id_email");

        builder.HasIndex(u => u.TenantId)
            .HasDatabaseName("ix_users_tenant_id");

        builder.HasIndex(u => u.Email)
            .HasDatabaseName("ix_users_email");

        builder.HasIndex(u => u.IsActive)
            .HasDatabaseName("ix_users_is_active");

        // Relationships (Tenant relationship is configured in TenantConfiguration)
        // Employee relationship is configured in EmployeeConfiguration (one-to-one)
    }
}

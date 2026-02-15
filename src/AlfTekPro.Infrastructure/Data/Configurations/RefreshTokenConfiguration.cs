using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AlfTekPro.Domain.Entities.Platform;

namespace AlfTekPro.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for RefreshToken
/// </summary>
public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");

        // Primary Key
        builder.HasKey(rt => rt.Id);
        builder.Property(rt => rt.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        // Properties
        builder.Property(rt => rt.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(rt => rt.Token)
            .HasColumnName("token")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(rt => rt.ExpiresAt)
            .HasColumnName("expires_at")
            .IsRequired();

        builder.Property(rt => rt.RevokedAt)
            .HasColumnName("revoked_at");

        builder.Property(rt => rt.CreatedByIp)
            .HasColumnName("created_by_ip")
            .HasMaxLength(45); // IPv6 max length

        builder.Property(rt => rt.RevokedByIp)
            .HasColumnName("revoked_by_ip")
            .HasMaxLength(45);

        builder.Property(rt => rt.ReplacedByToken)
            .HasColumnName("replaced_by_token")
            .HasMaxLength(500);

        builder.Property(rt => rt.RevocationReason)
            .HasColumnName("revocation_reason")
            .HasMaxLength(200);

        builder.Property(rt => rt.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(rt => rt.UpdatedAt)
            .HasColumnName("updated_at");

        // Relationships
        builder.HasOne(rt => rt.User)
            .WithMany()
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(rt => rt.Token)
            .HasDatabaseName("ix_refresh_tokens_token")
            .IsUnique();

        builder.HasIndex(rt => rt.UserId)
            .HasDatabaseName("ix_refresh_tokens_user_id");

        builder.HasIndex(rt => rt.ExpiresAt)
            .HasDatabaseName("ix_refresh_tokens_expires_at");

        // Computed columns are not mapped (IsActive, IsExpired)
        builder.Ignore(rt => rt.IsActive);
        builder.Ignore(rt => rt.IsExpired);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities;

namespace Infrastructure.Data.Configurations.General;

public class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
{
    public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
    {
        builder.ToTable("password_reset_tokens");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id");

        builder.Property(p => p.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(p => p.Token)
            .HasColumnName("token")
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(p => p.ExpiresAt)
            .HasColumnName("expires_at")
            .IsRequired();

        builder.Property(p => p.IsUsed)
            .HasColumnName("is_used")
            .IsRequired();

        builder.Property(p => p.UsedAt)
            .HasColumnName("used_at");

        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(p => p.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.Property(p => p.Version)
            .IsRowVersion()
            .IsConcurrencyToken()
            .HasColumnName("version")
            .HasDefaultValueSql("gen_random_bytes(8)");

        // Indexes
        builder.HasIndex(p => p.UserId);
        builder.HasIndex(p => p.Token).IsUnique();
        builder.HasIndex(p => new { p.UserId, p.IsUsed, p.ExpiresAt });

        builder.Ignore(p => p.CreatorId);
    }
}

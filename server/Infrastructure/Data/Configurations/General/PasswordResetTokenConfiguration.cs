using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities;

namespace Infrastructure.Data.Configurations;

public class PasswordResetTokenConfiguration : EntityConfiguration<PasswordResetToken>
{
    public override void Configure(EntityTypeBuilder<PasswordResetToken> builder)
    {
        base.Configure(builder);

        builder.ToTable("password_reset_tokens");

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

        // Indexes
        builder.HasIndex(p => p.UserId);
        builder.HasIndex(p => p.Token).IsUnique();
        builder.HasIndex(p => new { p.UserId, p.IsUsed, p.ExpiresAt });

    }
}

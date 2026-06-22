using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Application;

public class SessionConfiguration : EntityConfiguration<Session>
{
    public override void Configure(EntityTypeBuilder<Session> builder)
    {
        base.Configure(builder);

        builder.ToTable("sessions");

        builder.Property(s => s.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(s => s.RefreshToken).HasColumnName("refresh_token").IsRequired().HasMaxLength(256);
        builder.Property(s => s.PreviousRefreshToken).HasColumnName("previous_refresh_token").HasMaxLength(256);
        builder.Property(s => s.ExpiresAt).HasColumnName("expires_at").IsRequired();
        builder.Property(x => x.RevokedAt).HasColumnName("revoked_at");
        builder.Property(x => x.UserAgent).HasColumnName("user_agent").HasMaxLength(500);
        builder.Property(x => x.IpAddress).HasColumnName("ip_address").HasMaxLength(45);
        builder.Property(x => x.LastTokenRotationAt).HasColumnName("last_token_rotation_at");

        // Indexes
        builder.HasIndex(s => s.UserId);
        builder.HasIndex (s => s.RefreshToken);
        builder.HasIndex (s => s.PreviousRefreshToken);
        builder.HasIndex(s => new { s.UserId, s.RevokedAt, s.ExpiresAt });

    }
}



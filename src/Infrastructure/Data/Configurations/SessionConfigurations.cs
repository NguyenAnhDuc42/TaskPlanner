using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using src.Domain.Entities.SessionEntity;

namespace src.Infrastructure.Data.Configurations;

public class SessionConfigurations : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.ToTable("Sessions");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.RefreshToken)
            .IsRequired()
            .HasMaxLength(500);
        builder.Property(s => s.UserId).IsRequired();
        builder.Property(s => s.ExspireAt)
            .IsRequired();
        builder.Property(s => s.RevokedAt)
            .IsRequired(false);
        builder.Property(s => s.UserAgent)
            .IsRequired()
            .HasMaxLength(200);
        builder.Property(s => s.IpAddress)
            .IsRequired()
            .HasMaxLength(50); // IPv6 max length is 45 characters

        // Indexes
        builder.HasIndex(s => s.RefreshToken)
            .IsUnique();
        builder.HasIndex(s => s.UserId);
        
    }
}

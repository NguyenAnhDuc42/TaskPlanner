using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities;

namespace Infrastructure.Data.Configurations.General;

public class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.ToTable("sessions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.UserId)
            .IsRequired();

        builder.Property(s => s.RefreshToken)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(s => s.ExpiresAt)
            .IsRequired();

        builder.Property(s => s.IsActive)
            .IsRequired();

    }
}

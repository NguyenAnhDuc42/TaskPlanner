using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities;

namespace Infrastructure.Data.Configurations;

public class UserConfiguration : EntityConfiguration<User>
{
    public override void Configure(EntityTypeBuilder<User> builder)
    {
        base.Configure(builder);

        builder.ToTable("users");

        builder.Property(u => u.Name)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnName("name");

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(255)
            .HasColumnName("email");

        builder.HasIndex(u => u.Email).IsUnique();

        builder.Property(u => u.PasswordHash)
            .HasMaxLength(500)
            .HasColumnName("password_hash");

        builder.Property(u => u.AuthProvider)
            .HasMaxLength(50)
            .HasColumnName("auth_provider");

        builder.Property(u => u.ExternalId)
            .HasMaxLength(255)
            .HasColumnName("external_id");

        builder.HasIndex(u => new { u.AuthProvider, u.ExternalId }).IsUnique();

    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities; // For User
using Domain.Enums; // For Role

namespace Infrastructure.Data.Configurations.General;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.HasIndex(u => u.Email)
            .IsUnique(); // Email should be unique

        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(u => u.FirstName)
            .HasMaxLength(100);

        builder.Property(u => u.LastName)
            .HasMaxLength(100);

        builder.Property(u => u.Role)
            .IsRequired()
            .HasConversion<string>(); // Store enum as string

        builder.Property(u => u.ProfilePictureUrl)
            .HasMaxLength(2048);

        builder.Property(u => u.LastLogin)
            .IsRequired(false); // Nullable

        builder.Property(u => u.IsActive)
            .IsRequired();

        // Configure common Entity properties
        builder.Property(e => e.Version)
            .IsRowVersion();

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .IsRequired();

        // Ignore domain events collection as it's not persisted
        builder.Ignore(e => e.DomainEvents);
    }
}

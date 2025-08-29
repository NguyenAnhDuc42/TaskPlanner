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

        // Match domain User properties
        builder.Property(u => u.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.HasIndex(u => u.Email)
            .IsUnique(); // Email should be unique

        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(256);

     
    // Ignore domain events collection as it's an in-memory concern on Aggregate
    builder.Ignore(u => u.DomainEvents);
    }
}

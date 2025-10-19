using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities; // For User
using Domain.Enums; // For Role

namespace Infrastructure.Data.Configurations.General;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasColumnName("id");
        builder.Property(u => u.Name).IsRequired().HasMaxLength(200).HasColumnName("name");
        builder.Property(u => u.Email).IsRequired().HasMaxLength(256).HasColumnName("email");
        builder.Property(u => u.PasswordHash).IsRequired().HasMaxLength(256).HasColumnName("password_hash");

        builder.HasIndex(u => u.Email).IsUnique(); // Email should be unique


        builder.Ignore(u => u.DomainEvents);
    }
}

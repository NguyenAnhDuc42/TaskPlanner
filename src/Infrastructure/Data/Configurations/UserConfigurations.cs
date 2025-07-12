using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using src.Domain.Entities.UserEntity;
using src.Domain.Valueobject;
using System;

namespace Infastructure.DBContext.Configurations;

public class UserConfigurations : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Name);
        builder.Property(u => u.Email)
            .HasConversion(vl => vl.Value, vl => new Email(vl))
            .IsRequired();
        builder.Property(u => u.PasswordHash)
            .IsRequired();


        //index
        builder.HasIndex(u => u.Email).IsUnique();
        
        //relationships
        builder.HasMany(u => u.Sessions)
            .WithOne()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(u => u.Workspaces)
            .WithOne()
            .HasForeignKey(uw => uw.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(u => u.Spaces)
            .WithOne()
            .HasForeignKey(uw => uw.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(u => u.Folders)
            .WithOne()
            .HasForeignKey(uw => uw.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(u => u.Lists)
            .WithOne()
            .HasForeignKey(uw => uw.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(u => u.Tasks)
            .WithOne()
            .HasForeignKey(uw => uw.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        
        //ignore
        builder.Ignore(u => u.DomainEvents);

    }
}

using System;
using Domain.Entities;
using Domain.Entities.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations.Support;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Message)
               .HasMaxLength(1000)
               .IsRequired();

        builder.Property(x => x.IsRead)
               .IsRequired();

        builder.HasOne<User>()
               .WithMany(u => u.Notifications)
               .HasForeignKey(x => x.RecipientId);

        builder.HasOne<User>()
               .WithMany()
               .HasForeignKey(x => x.TriggeredByUserId);

        // RelatedEntityId stays flexible (can point to task/comment/etc.)
    }
}

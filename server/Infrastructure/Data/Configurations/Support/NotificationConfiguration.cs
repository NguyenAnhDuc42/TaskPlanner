using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities.Support;

namespace Infrastructure.Data.Configurations.Support;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.UserId)
            .IsRequired();

        builder.Property(n => n.Message)
            .IsRequired()
            .HasMaxLength(1024);

        builder.Property(n => n.IsRead)
            .IsRequired();

        builder.Property(n => n.RelatedEntityId)
            .IsRequired(false); // Nullable

        builder.Property(n => n.RelatedEntityType)
            .HasMaxLength(100); // Store type name as string

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

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities.Support;

namespace Infrastructure.Data.Configurations.Support;

public class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.ToTable("Tags");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(t => t.Color)
            .HasMaxLength(50);

        // Removed: builder.Property(t => t.ProjectWorkspaceId).IsRequired();
        // Removed: builder.Property(e => e.Version).IsRowVersion();
        // Removed: builder.Property(e => e.CreatedAt).IsRequired();
        // Removed: builder.Property(e => e.UpdatedAt).IsRequired();
        // Removed: builder.Ignore(e => e.DomainEvents);
    }
}

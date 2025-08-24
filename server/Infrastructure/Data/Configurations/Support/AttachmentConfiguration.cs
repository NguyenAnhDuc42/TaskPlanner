using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities.Support;

namespace Infrastructure.Data.Configurations.Support;

public class AttachmentConfiguration : IEntityTypeConfiguration<Attachment>
{
    public void Configure(EntityTypeBuilder<Attachment> builder)
    {
        builder.ToTable("Attachments");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.FileName)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(a => a.FileUrl)
            .IsRequired()
            .HasMaxLength(2048); // URL can be long

        builder.Property(a => a.FileType)
            .HasMaxLength(50);

        builder.Property(a => a.UploaderId)
            .IsRequired();

        builder.Property(a => a.ProjectTaskId)
            .IsRequired();

        // Configure common Entity properties
        builder.Property(e => e.Version)
            .IsRowVersion();

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .IsRequired();

    // Attachment inherits Entity which contains Version/CreatedAt/UpdatedAt - no DomainEvents on Entity
    }
}

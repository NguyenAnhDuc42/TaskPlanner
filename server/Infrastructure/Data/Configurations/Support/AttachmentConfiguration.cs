using System;
using Domain.Entities;
using Domain.Entities.ProjectEntities;
using Domain.Entities.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations.Support;

public class AttachmentConfiguration : IEntityTypeConfiguration<Attachment>
{
    public void Configure(EntityTypeBuilder<Attachment> builder)
    {
        builder.ToTable("Attachments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.FileName)
               .HasMaxLength(255)
               .IsRequired();

        builder.Property(x => x.FileUrl)
               .HasMaxLength(500)
               .IsRequired();

        builder.Property(x => x.FileType)
               .HasMaxLength(100);

        builder.Property(x => x.FileSize)
               .IsRequired();

        builder.HasOne<ProjectTask>()
               .WithMany(t => t.Attachments)
               .HasForeignKey(x => x.ProjectTaskId);

        builder.HasOne<User>()
               .WithMany()
               .HasForeignKey(x => x.UploadedById);
    }
}
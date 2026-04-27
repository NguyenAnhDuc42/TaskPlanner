using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities;

namespace Infrastructure.Data.Configurations;

public class AttachmentLinkConfiguration : EntityConfiguration<AttachmentLink>
{
    public override void Configure(EntityTypeBuilder<AttachmentLink> builder)
    {
        base.Configure(builder);

        builder.ToTable("attachment_links");

        builder.Property(x => x.AttachmentId).HasColumnName("attachment_id").IsRequired();
        builder.Property(x => x.ProjectSpaceId).HasColumnName("project_space_id");
        builder.Property(x => x.ProjectFolderId).HasColumnName("project_folder_id");
        builder.Property(x => x.ProjectTaskId).HasColumnName("project_task_id");
        builder.Property(x => x.CommentId).HasColumnName("comment_id");

        // Indexes for common queries
        builder.HasIndex(x => x.AttachmentId);
        builder.HasIndex(x => x.ProjectSpaceId);
        builder.HasIndex(x => x.ProjectFolderId);
        builder.HasIndex(x => x.ProjectTaskId);
        builder.HasIndex(x => x.CommentId);

    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities.ProjectEntities;

namespace Infrastructure.Data.Configurations.ProjectEntities;

public class CommentConfiguration : EntityConfiguration<Comment>
{
    public override void Configure(EntityTypeBuilder<Comment> builder)
    {
        base.Configure(builder);

        builder.ToTable("comments");

        builder.Property(x => x.ProjectTaskId).HasColumnName("project_task_id").IsRequired();
        builder.Property(x => x.Content).HasColumnName("content").HasMaxLength(2000).IsRequired();
        builder.Property(x => x.IsEdited).HasColumnName("is_edited").IsRequired();
        builder.Property(x => x.ParentCommentId).HasColumnName("parent_comment_id");

        builder.HasIndex(x => x.ProjectTaskId);
    }
}

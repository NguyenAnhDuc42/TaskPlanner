using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities;

namespace Infrastructure.Data.Configurations;

public class EntityAccessConfiguration : EntityConfiguration<EntityAccess>
{
    public override void Configure(EntityTypeBuilder<EntityAccess> builder)
    {
        base.Configure(builder);

        builder.ToTable("entity_access");

        builder.Property(x => x.ProjectWorkspaceId).HasColumnName("project_workspace_id").IsRequired();
        builder.Property(x => x.WorkspaceMemberId).HasColumnName("workspace_member_id").IsRequired();
        
        builder.Property(x => x.ProjectSpaceId).HasColumnName("project_space_id");
        builder.Property(x => x.ProjectFolderId).HasColumnName("project_folder_id");
        builder.Property(x => x.ProjectTaskId).HasColumnName("project_task_id");
        builder.Property(x => x.AccessLevel).HasColumnName("access_level").HasConversion<string>().HasMaxLength(50).IsRequired();

        // Indexes for common queries
        builder.HasIndex(x => x.ProjectWorkspaceId);
        builder.HasIndex(x => x.WorkspaceMemberId);
        builder.HasIndex(x => x.ProjectSpaceId);
        builder.HasIndex(x => x.ProjectFolderId);
        builder.HasIndex(x => x.ProjectTaskId);

    }
}
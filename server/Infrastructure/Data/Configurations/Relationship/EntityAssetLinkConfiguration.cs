using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities;

namespace Infrastructure.Data.Configurations;

public class EntityAssetLinkConfiguration : EntityConfiguration<EntityAssetLink>
{
    public override void Configure(EntityTypeBuilder<EntityAssetLink> builder)
    {
        base.Configure(builder);

        builder.ToTable("entity_asset_links");

        builder.Property(x => x.ProjectWorkspaceId).HasColumnName("project_workspace_id").IsRequired();

        builder.Property(x => x.AssetId).HasColumnName("asset_id").IsRequired();
        builder.Property(x => x.AssetType).HasColumnName("asset_type").HasConversion<string>().HasMaxLength(100).IsRequired();
        
        builder.Property(x => x.ProjectSpaceId).HasColumnName("project_space_id");
        builder.Property(x => x.ProjectFolderId).HasColumnName("project_folder_id");
        builder.Property(x => x.ProjectTaskId).HasColumnName("project_task_id");
        builder.Property(x => x.CommentId).HasColumnName("comment_id");

        // High-Performance Indexes for Hierarchy lookups
        builder.HasIndex(x => x.AssetId);
        builder.HasIndex(x => x.ProjectSpaceId);
        builder.HasIndex(x => x.ProjectFolderId);
        builder.HasIndex(x => x.ProjectTaskId);
        builder.HasIndex(x => x.CommentId);

    }
}

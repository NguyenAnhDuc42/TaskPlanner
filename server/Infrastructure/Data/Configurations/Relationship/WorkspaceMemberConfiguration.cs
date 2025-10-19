using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities.Relationship;
using Domain.Entities.ProjectEntities;

namespace Infrastructure.Data.Configurations.Relationship;


public class WorkspaceMemberConfiguration : CompositeConfiguration<WorkspaceMember>
{
    public override void Configure(EntityTypeBuilder<WorkspaceMember> builder)
    {
        base.Configure(builder);

        builder.ToTable("workspace_members");

        // Composite PK: Workspace + User (one membership row per user per workspace)
        builder.HasKey(x => new { x.ProjectWorkspaceId, x.UserId });

        builder.Property(x => x.UserId).IsRequired().HasColumnName("user_id");
        builder.Property(x => x.ProjectWorkspaceId).IsRequired().HasColumnName("project_workspace_id");
        builder.Property(x => x.Role).HasConversion<string>().HasMaxLength(50).HasColumnName("role").IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(50).HasColumnName("status").IsRequired();
        builder.Property(x => x.CreatedBy).IsRequired().HasColumnName("created_by");
        builder.Property(x => x.JoinedAt).IsRequired().HasColumnName("joined_at");
        builder.Property(x => x.ApprovedAt).HasColumnName("approved_at");
        builder.Property(x => x.ApprovedBy).HasColumnName("approved_by");
        builder.Property(x => x.SuspendedAt).HasColumnName("suspended_at");
        builder.Property(x => x.SuspendedBy).HasColumnName("suspended_by");
        builder.Property(x => x.JoinMethod).HasMaxLength(64).HasColumnName("join_method");

        // Indexes for lookups and admin queries
        builder.HasIndex(x => x.ProjectWorkspaceId);
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => new { x.ProjectWorkspaceId, x.Status });

        builder.HasOne<ProjectWorkspace>().WithMany().HasForeignKey(x => x.ProjectWorkspaceId).OnDelete(DeleteBehavior.Cascade);
    }
}

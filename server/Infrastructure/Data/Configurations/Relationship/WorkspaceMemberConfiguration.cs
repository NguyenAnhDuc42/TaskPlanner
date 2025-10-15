using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities.Relationship;

namespace Infrastructure.Data.Configurations.Relationship;


public class WorkspaceMemberConfiguration : CompositeConfiguration<WorkspaceMember>
{
    public override void Configure(EntityTypeBuilder<WorkspaceMember> builder)
    {
        base.Configure(builder);

        builder.ToTable("workspace_members");

        // Composite PK: Workspace + User (one membership row per user per workspace)
        builder.HasKey(x => new { x.ProjectWorkspaceId, x.UserId });

        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.ProjectWorkspaceId).IsRequired();
        builder.Property(x => x.Role).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.CreatedBy).IsRequired();
        builder.Property(x => x.JoinedAt).IsRequired();
        builder.Property(x => x.ApprovedAt);
        builder.Property(x => x.ApprovedBy);
        builder.Property(x => x.SuspendedAt);
        builder.Property(x => x.SuspendedBy);
        builder.Property(x => x.JoinMethod).HasMaxLength(64);

        // Indexes for lookups and admin queries
        builder.HasIndex(x => x.ProjectWorkspaceId);
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => new { x.ProjectWorkspaceId, x.Status });
    }
}

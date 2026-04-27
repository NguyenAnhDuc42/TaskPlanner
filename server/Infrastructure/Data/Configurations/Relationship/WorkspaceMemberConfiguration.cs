using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities;
using Domain.Common;

namespace Infrastructure.Data.Configurations;

public class WorkspaceMemberConfiguration : EntityConfiguration<WorkspaceMember>
{
    public override void Configure(EntityTypeBuilder<WorkspaceMember> builder)
    {
        base.Configure(builder);

        builder.ToTable("workspace_members");

        builder.Property(x => x.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(x => x.ProjectWorkspaceId)
            .HasColumnName("project_workspace_id")
            .IsRequired();

        builder.Property(x => x.Role)
            .HasColumnName("role")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();
        
        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.JoinedAt)
            .HasColumnName("joined_at");

        builder.Property(x => x.SuspendedAt)
            .HasColumnName("suspended_at");

        builder.Property(x => x.SuspendedBy)
            .HasColumnName("suspended_by");

        builder.Property(x => x.IsPinned)
            .HasColumnName("is_pinned");

        builder.Property(x => x.Theme)
            .HasColumnName("theme")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired()
            .HasDefaultValue(Domain.Enums.Theme.Dark);

        builder.Property(x => x.JoinMethod)
            .HasColumnName("join_method")
            .HasMaxLength(64);

        // Indexes
        builder.HasIndex(x => x.ProjectWorkspaceId);
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => new { x.UserId, x.ProjectWorkspaceId });

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

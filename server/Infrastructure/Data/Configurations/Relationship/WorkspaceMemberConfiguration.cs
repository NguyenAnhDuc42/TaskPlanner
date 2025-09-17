using System;
using Domain.Entities.Relationship;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations.Relationship;

public class WorkspaceMemberConfiguration : CompositeConfiguration<WorkspaceMember>
{
    public override void Configure(EntityTypeBuilder<WorkspaceMember> builder)
    {
        base.Configure(builder); 

        // Composite PK
        builder.HasKey(x => new { x.UserId, x.ProjectWorkspaceId });

        // Domain-specific config
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.ProjectWorkspaceId).IsRequired();
        builder.Property(x => x.Role).IsRequired();

        builder.Property(x => x.IsPending)
            .IsRequired()
            .HasDefaultValue(false);

        builder.HasIndex(x => new { x.ProjectWorkspaceId, x.Role });
    }
}

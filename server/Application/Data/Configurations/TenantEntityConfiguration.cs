using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Application;

public abstract class TenantEntityConfiguration<TEntity> : EntityConfiguration<TEntity>
    where TEntity : TenantEntity
{
    public override void Configure(EntityTypeBuilder<TEntity> builder)
    {
        base.Configure(builder);

        builder.Property(t => t.ProjectWorkspaceId)
            .HasColumnName("project_workspace_id")
            .IsRequired();

        builder.HasOne<ProjectWorkspace>()
            .WithMany()
            .HasForeignKey(t => t.ProjectWorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => t.ProjectWorkspaceId);
    }
}



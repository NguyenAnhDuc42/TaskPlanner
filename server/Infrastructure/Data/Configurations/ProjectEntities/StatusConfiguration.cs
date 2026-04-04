using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities.ProjectEntities;

namespace Infrastructure.Data.Configurations.ProjectEntities;

public class StatusConfiguration : EntityConfiguration<Status>
{
    public override void Configure(EntityTypeBuilder<Status> builder)
    {
        base.Configure(builder);

        builder.ToTable("statuses");

        builder.Property(x => x.ProjectWorkspaceId).HasColumnName("project_workspace_id").IsRequired();
        builder.Property(x => x.WorkflowId).HasColumnName("workflow_id").IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(x => x.Color).HasColumnName("color").HasMaxLength(32).IsRequired();
        builder.Property(x => x.Category).HasColumnName("category").HasConversion<string>().HasMaxLength(50).IsRequired();

        // Indexes
        builder.HasIndex(x => x.ProjectWorkspaceId);
        builder.HasIndex(x => x.WorkflowId);
        
        // Relationships
        builder.HasOne<Workflow>()
            .WithMany(w => w.Statuses)
            .HasForeignKey(x => x.WorkflowId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

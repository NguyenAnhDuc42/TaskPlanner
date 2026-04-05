using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities.ProjectEntities;

namespace Infrastructure.Data.Configurations.ProjectEntities;

public class WorkflowConfiguration : EntityConfiguration<Workflow>
{
    public override void Configure(EntityTypeBuilder<Workflow> builder)
    {
        base.Configure(builder);

        builder.ToTable("workflows");

        builder.Property(x => x.ProjectWorkspaceId).HasColumnName("project_workspace_id").IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(x => x.Description).HasColumnName("description").HasMaxLength(500);
    
        builder.HasIndex(x => x.ProjectWorkspaceId);
            
        builder.HasMany(x => x.Statuses)
            .WithOne()
            .HasForeignKey(x => x.WorkflowId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

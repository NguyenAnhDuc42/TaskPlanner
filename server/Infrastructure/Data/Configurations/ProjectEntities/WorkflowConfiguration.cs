using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities;
using Domain.Common;

namespace Infrastructure.Data.Configurations;

public class WorkflowConfiguration : TenantEntityConfiguration<Workflow>
{
    public override void Configure(EntityTypeBuilder<Workflow> builder)
    {
        base.Configure(builder);

        builder.ToTable("workflows");

        builder.Property(w => w.ProjectSpaceId)
            .HasColumnName("project_space_id");

        builder.Property(w => w.ProjectFolderId)
            .HasColumnName("project_folder_id");

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasColumnName("description")
            .HasMaxLength(500);
            
        builder.HasMany(x => x.Statuses)
            .WithOne()
            .HasForeignKey(x => x.WorkflowId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ProjectSpace>()
            .WithMany()
            .HasForeignKey(w => w.ProjectSpaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ProjectFolder>()
            .WithMany()
            .HasForeignKey(w => w.ProjectFolderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

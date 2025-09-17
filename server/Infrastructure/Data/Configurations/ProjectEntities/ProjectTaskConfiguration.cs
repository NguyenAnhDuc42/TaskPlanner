using Domain.Entities.ProjectEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations.ProjectEntities;

public class ProjectTaskConfiguration : EntityConfiguration<ProjectTask>
{
    public override void Configure(EntityTypeBuilder<ProjectTask> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.ProjectListId).IsRequired();

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.CreatorId).IsRequired();

        builder.HasIndex(x => new { x.ProjectListId, x.Name });
    }
}
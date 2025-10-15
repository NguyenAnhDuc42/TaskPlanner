using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities.ProjectEntities;

namespace Infrastructure.Data.Configurations.ProjectEntities;

public class ProjectTaskConfiguration : EntityConfiguration<ProjectTask>
{
    public override void Configure(EntityTypeBuilder<ProjectTask> builder)
    {
        base.Configure(builder);

        builder.ToTable("project_tasks");

        builder.Property(x => x.ProjectListId).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(4000);
        builder.Property(x => x.CreatorId).IsRequired();
        builder.Property(x => x.StatusId);
        builder.Property(x => x.IsArchived).IsRequired();
        builder.Property(x => x.Priority).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.StartDate);
        builder.Property(x => x.DueDate);
        builder.Property(x => x.StoryPoints);
        builder.Property(x => x.TimeEstimate);
        builder.Property(x => x.OrderKey);

        builder.OwnsOne(x => x.Customization, cb =>
        {
            cb.Property(p => p.Color).HasColumnName("custom_color").HasMaxLength(32).IsRequired();
            cb.Property(p => p.Icon).HasColumnName("custom_icon").HasMaxLength(128).IsRequired();
        });

        builder.HasIndex(x => x.ProjectListId);
        builder.HasIndex(x => new { x.ProjectListId, x.StatusId });
        builder.HasIndex(x => new { x.ProjectListId, x.OrderKey });
        builder.HasIndex(x => x.DueDate);
    }
}

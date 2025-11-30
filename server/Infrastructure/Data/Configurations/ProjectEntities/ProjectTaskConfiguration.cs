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

        builder.Property(x => x.ProjectListId).HasColumnName("project_list_id").IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(300).IsRequired();
        builder.Property(x => x.Description).HasColumnName("description").HasMaxLength(4000);
        builder.Property(x => x.StatusId).HasColumnName("status_id");
        builder.Property(x => x.IsArchived).HasColumnName("is_archived").IsRequired();
        builder.Property(x => x.Priority).HasColumnName("priority").HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.StartDate).HasColumnName("start_date");
        builder.Property(x => x.DueDate).HasColumnName("due_date");
        builder.Property(x => x.StoryPoints).HasColumnName("story_points");
        builder.Property(x => x.TimeEstimate).HasColumnName("time_estimate");
        builder.Property(x => x.OrderKey).HasColumnName("order_key");

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

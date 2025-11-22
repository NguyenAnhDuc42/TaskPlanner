using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities.Support;

namespace Infrastructure.Data.Configurations.Support.Task;

public class StatusConfiguration : EntityConfiguration<Status>
{
    public override void Configure(EntityTypeBuilder<Status> builder)
    {
        base.Configure(builder);

        builder.ToTable("statuses");

        builder.Property(x => x.ProjectWorkspaceId).HasColumnName("project_workspace_id").IsRequired();
        builder.Property(x => x.ProjectSpaceId).HasColumnName("project_space_id");
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(x => x.Color).HasColumnName("color").HasMaxLength(32).IsRequired();
        builder.Property(x => x.OrderKey).HasColumnName("order_key").IsRequired();
        builder.Property(x => x.IsDefaultStatus).HasColumnName("is_default_status").IsRequired();

        builder.HasIndex(x => x.ProjectWorkspaceId);
        builder.HasIndex(x => x.ProjectSpaceId);
    }
}

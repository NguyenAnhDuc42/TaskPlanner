using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities.ProjectEntities;

namespace Infrastructure.Data.Configurations.ProjectEntities;

public class ProjectWorkspaceConfiguration : EntityConfiguration<ProjectWorkspace>
{
    public override void Configure(EntityTypeBuilder<ProjectWorkspace> builder)
    {
        base.Configure(builder);

        builder.ToTable("project_workspaces");

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.JoinCode).HasMaxLength(32).IsRequired();
        builder.Property(x => x.StrictJoin).IsRequired();
        builder.Property(x => x.IsArchived).IsRequired();
        builder.Property(x => x.CreatorId).IsRequired();

        // enums as strings
        builder.Property(x => x.Theme).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.Variant).HasConversion<string>().HasMaxLength(50).IsRequired();

        // owned VO
        builder.OwnsOne(x => x.Customization, cb =>
        {
            cb.Property(p => p.Color).HasColumnName("custom_color").HasMaxLength(32).IsRequired();
            cb.Property(p => p.Icon).HasColumnName("custom_icon").HasMaxLength(128).IsRequired();
        });

        // Indexes
        builder.HasIndex(x => x.CreatorId);
        builder.HasIndex(x => x.JoinCode).IsUnique(true);

        // optional: searchable name
        builder.HasIndex(x => x.Name);
    }
}

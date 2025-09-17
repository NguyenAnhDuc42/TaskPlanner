using Domain.Entities.ProjectEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations.ProjectEntities;

public class ProjectWorkspaceConfiguration : EntityConfiguration<ProjectWorkspace>
{
    public override void Configure(EntityTypeBuilder<ProjectWorkspace> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        builder.Property(x => x.JoinCode)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(x => x.JoinCode).IsUnique();

        builder.Property(x => x.Color).HasMaxLength(20);
        builder.Property(x => x.Icon).HasMaxLength(50);

        builder.Property(x => x.CreatorId).IsRequired();
    }
}
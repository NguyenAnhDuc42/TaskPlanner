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

        builder.Property(w => w.Id)
            .HasColumnName("id")
            .HasColumnOrder(0);

        builder.Property(w => w.Name)
            .HasColumnName("name")
            .IsRequired()
            .HasMaxLength(150)
            .HasColumnOrder(1);

        builder.Property(w => w.Slug)
            .HasColumnName("slug")
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnOrder(2);
        
        builder.HasIndex(w => w.Slug).IsUnique();

        builder.Property(w => w.Description)
            .HasColumnName("description")
            .HasColumnType("jsonb")
            .HasColumnOrder(3);

        builder.Property(w => w.JoinCode)
            .HasColumnName("join_code")
            .IsRequired()
            .HasMaxLength(32)
            .HasColumnOrder(4);

        builder.OwnsOne(w => w.Customization, c =>
        {
            c.Property(cust => cust.Color).HasColumnName("custom_color").HasColumnOrder(5);
            c.Property(cust => cust.Icon).HasColumnName("custom_icon").HasColumnOrder(6);
        });

        builder.Property(w => w.Theme)
            .HasColumnName("theme")
            .HasConversion<string>()
            .HasMaxLength(50)
            .HasColumnOrder(7);

        builder.Property(w => w.StrictJoin)
            .HasColumnName("strict_join")
            .HasColumnOrder(8);

        builder.Property(w => w.IsArchived)
            .HasColumnName("is_archived")
            .HasColumnOrder(9);

        // Auditing (Overrides from base to set order)
        builder.Property(w => w.CreatedAt).HasColumnOrder(10);
        builder.Property(w => w.UpdatedAt).HasColumnOrder(11);
        builder.Property(w => w.DeletedAt).HasColumnOrder(12);
        builder.Property(w => w.CreatorId).HasColumnOrder(13);
    }
}

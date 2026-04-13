using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities;

namespace Infrastructure.Data.Configurations.ProjectEntities;

public class ProjectFolderConfiguration : EntityConfiguration<ProjectFolder>
{
    public override void Configure(EntityTypeBuilder<ProjectFolder> builder)
    {
        base.Configure(builder);

        builder.ToTable("project_folders");

        builder.Property(f => f.Id)
            .HasColumnName("id")
            .HasColumnOrder(0);

        builder.Property(f => f.ProjectSpaceId)
            .HasColumnName("project_space_id")
            .HasColumnOrder(1);

        builder.Property(f => f.Name)
            .HasColumnName("name")
            .IsRequired()
            .HasMaxLength(150)
            .HasColumnOrder(2);

        builder.Property(f => f.Slug)
            .HasColumnName("slug")
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnOrder(3);

        builder.HasIndex(f => new { f.ProjectSpaceId, f.Slug }).IsUnique();

        builder.Property(f => f.Description)
            .HasColumnName("description")
            .HasColumnType("jsonb")
            .HasColumnOrder(4);

        builder.Property(f => f.OrderKey)
            .HasColumnName("order_key")
            .HasColumnOrder(5);

        builder.Property(f => f.IsPrivate)
            .HasColumnName("is_private")
            .HasColumnOrder(6);

        builder.Property(f => f.IsArchived)
            .HasColumnName("is_archived")
            .HasColumnOrder(7);

        builder.Property(f => f.StartDate)
            .HasColumnName("start_date")
            .HasColumnOrder(8);

        builder.Property(f => f.DueDate)
            .HasColumnName("due_date")
            .HasColumnOrder(9);

        builder.OwnsOne(f => f.Customization, c =>
        {
            c.Property(cust => cust.Color).HasColumnName("custom_color").HasColumnOrder(10);
            c.Property(cust => cust.Icon).HasColumnName("custom_icon").HasColumnOrder(11);
        });

        // Auditing (Overrides from base to set order)
        builder.Property(f => f.CreatedAt).HasColumnName("created_at").HasColumnOrder(12);
        builder.Property(f => f.UpdatedAt).HasColumnName("updated_at").HasColumnOrder(13);
        builder.Property(f => f.DeletedAt).HasColumnName("deleted_at").HasColumnOrder(14);
        builder.Property(f => f.CreatorId).HasColumnName("creator_id").HasColumnOrder(15);

        builder.HasIndex(x => x.ProjectSpaceId);

        // builder.HasOne<ProjectSpace>()
        //     .WithMany()
        //     .HasForeignKey(x => x.ProjectSpaceId)
        //     .OnDelete(DeleteBehavior.Cascade);
    }
}

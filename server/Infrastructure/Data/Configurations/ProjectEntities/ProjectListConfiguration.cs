using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities.ProjectEntities;
using Org.BouncyCastle.Math.EC.Rfc7748;

namespace Infrastructure.Data.Configurations.ProjectEntities;

public class ProjectListConfiguration : EntityConfiguration<ProjectList>
{
    public override void Configure(EntityTypeBuilder<ProjectList> builder)
    {
        base.Configure(builder);

        builder.ToTable("project_lists");

        builder.Property(x => x.ProjectSpaceId).HasColumnName("project_space_id").IsRequired();
        builder.Property(x => x.ProjectFolderId).HasColumnName("project_folder_id");
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(x => x.OrderKey).HasColumnName("order_key");
        builder.Property(x => x.IsPrivate).HasColumnName("is_private").IsRequired();
        builder.Property(x => x.IsArchived).HasColumnName("is_archived").IsRequired();
        builder.Property(x => x.InheritStatus).HasColumnName("inherit_status").IsRequired();
        builder.Property(x => x.StartDate).HasColumnName("start_date");
        builder.Property(x => x.DueDate).HasColumnName("due_date");
        builder.Property(x => x.NextItemOrder).HasColumnName("next_item_order").IsRequired();

        builder.OwnsOne(x => x.Customization, cb =>
        {
            cb.Property(p => p.Color).HasColumnName("custom_color").HasMaxLength(32).IsRequired();
            cb.Property(p => p.Icon).HasColumnName("custom_icon").HasMaxLength(128).IsRequired();
        });

        builder.HasIndex(x => x.ProjectSpaceId);
        builder.HasIndex(x => new { x.ProjectSpaceId, x.ProjectFolderId });

        // builder.HasOne<ProjectSpace>()
        //     .WithMany()
        //     .HasForeignKey(x => x.ProjectSpaceId)
        //     .OnDelete(DeleteBehavior.Cascade);
        //  builder.HasOne<ProjectFolder>()
        //     .WithMany()
        //     .HasForeignKey(x => x.ProjectFolderId)
        //     .OnDelete(DeleteBehavior.Cascade);
        
    }
}

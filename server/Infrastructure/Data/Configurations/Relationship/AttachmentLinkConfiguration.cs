using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities.Relationship;

namespace Infrastructure.Data.Configurations.Relationship;

public class AttachmentLinkConfiguration : CompositeConfiguration<AttachmentLink>
{
    public override void Configure(EntityTypeBuilder<AttachmentLink> builder)
    {
        base.Configure(builder);

        builder.ToTable("attachment_links");

        // Composite PK to avoid duplicate links: (AttachmentId, ParentEntityType, ParentEntityId)
        builder.HasKey(x => new { x.AttachmentId, x.ParentEntityType, x.ParentEntityId });

        builder.Property(x => x.AttachmentId).HasColumnName("attachment_id").IsRequired();
        builder.Property(x => x.ParentEntityId).HasColumnName("parent_entity_id").IsRequired();
        builder.Property(x => x.ParentEntityType).HasColumnName("parent_entity_type").HasConversion<string>().HasMaxLength(100).IsRequired();
        builder.Property(x => x.CreatorId).HasColumnName("creator_id").IsRequired();

        // Indexes for common queries (list attachments for an owner; find links for garbage collection)
        builder.HasIndex(x => new { x.ParentEntityType, x.ParentEntityId });
        builder.HasIndex(x => x.AttachmentId);
        builder.HasIndex(x => x.CreatorId);

        // If you prefer a synthetic PK instead, replace HasKey(...) with a generated Id property on AttachmentLink.
    }
}

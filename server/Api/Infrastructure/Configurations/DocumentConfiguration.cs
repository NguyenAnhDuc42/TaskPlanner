using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Api;

public class DocumentConfiguration : TenantEntityConfiguration<Document>
{
    public override void Configure(EntityTypeBuilder<Document> builder)
    {
        base.Configure(builder);

        builder.ToTable("documents");

        builder.Property(d => d.Id).HasColumnName("id");
        builder.Property(d => d.ProjectWorkspaceId).HasColumnName("project_workspace_id");
        builder.Property(d => d.ProjectSpaceId).HasColumnName("project_space_id");
        builder.Property(d => d.ParentDocumentId).HasColumnName("parent_document_id");
        builder.Property(d => d.Name).HasColumnName("name").IsRequired().HasMaxLength(200);
        builder.Property(d => d.OrderKey).HasColumnName("order_key").IsRequired();
        builder.Property(d => d.Icon).HasColumnName("icon").HasMaxLength(64);
        builder.Property(d => d.Color).HasColumnName("color").HasMaxLength(16);

        builder.HasOne<ProjectSpace>()
            .WithMany()
            .HasForeignKey(d => d.ProjectSpaceId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasOne<Document>()
            .WithMany()
            .HasForeignKey(d => d.ParentDocumentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(d => new { d.ProjectSpaceId, d.ParentDocumentId, d.OrderKey, d.Id })
            .HasFilter("\"deleted_at\" IS NULL");

        builder.HasIndex(d => d.ParentDocumentId);
    }
}

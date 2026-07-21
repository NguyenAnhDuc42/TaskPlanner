using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Api;

public class DocumentBlockConfiguration : TenantEntityConfiguration<DocumentBlock>
{
    public override void Configure(EntityTypeBuilder<DocumentBlock> builder)
    {
        base.Configure(builder);

        builder.ToTable("document_blocks");

        builder.Property(x => x.DocumentId)
            .HasColumnName("document_id")
            .IsRequired();

        builder.HasIndex(x => x.DocumentId);

        builder.Property(x => x.Type)
            .HasConversion<string>()
            .HasColumnName("type")
            .IsRequired();

        builder.Property(x => x.Content)
            .HasColumnName("content")
            .IsRequired();

        builder.Property(x => x.OrderKey)
            .HasColumnName("order_key")
            .HasMaxLength(50)
            .IsRequired();
    }
}


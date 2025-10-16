using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities.Support;

namespace Infrastructure.Data.Configurations.Support;

public class AttachmentConfiguration : EntityConfiguration<Attachment>
{
    public override void Configure(EntityTypeBuilder<Attachment> builder)
    {
        base.Configure(builder);

        builder.ToTable("attachments");

        // Core storage fields
        builder.Property(x => x.ContentId).HasColumnName("content_id").HasMaxLength(512).IsRequired();
        builder.Property(x => x.StorageProvider).HasConversion<string>().HasColumnName("storage_provider").HasMaxLength(64).IsRequired();
        builder.Property(x => x.StoragePath).HasColumnName("storage_path").HasMaxLength(2000);

        // Visible metadata
        builder.Property(x => x.FileName).HasColumnName("file_name").HasMaxLength(512).IsRequired();
        builder.Property(x => x.ContentType).HasColumnName("content_type").HasMaxLength(256);
        builder.Property(x => x.SizeBytes).HasColumnName("size_bytes").IsRequired();
        builder.Property(x => x.Checksum).HasColumnName("checksum").HasMaxLength(256);
        builder.Property(x => x.ChecksumAlgorithm).HasColumnName("checksum_algorithm").HasMaxLength(64);

        // Lifecycle & operational
        builder.Property(x => x.ProcessingState).HasConversion<string>().HasColumnName("processing_state").HasMaxLength(64).IsRequired();
        builder.Property(x => x.IsPublic).HasColumnName("is_public").IsRequired();
        builder.Property(x => x.UploadedBy).HasColumnName("uploaded_by").IsRequired();
        builder.Property(x => x.LinkCount).HasColumnName("link_count").IsRequired();
        builder.Property(x => x.CustomMetaJson).HasColumnName("custom_meta_json").HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");

        // Indexes
        builder.HasIndex(x => x.ContentId).IsUnique();
        builder.HasIndex(x => x.UploadedBy);
        builder.HasIndex(x => x.ProcessingState);
        builder.HasIndex(x => x.LinkCount);

        // If you're on SQL Server, change custom_meta_json type mapping to nvarchar(max):
        // builder.Property(x => x.CustomMetaJson).HasColumnName("custom_meta_json").HasColumnType("nvarchar(max)").HasDefaultValue("{}");
    }
}

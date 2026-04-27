using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;
using Domain.Common;

namespace Infrastructure.Data.Configurations;

public class AttachmentConfiguration : TenantEntityConfiguration<Attachment>
{
    public override void Configure(EntityTypeBuilder<Attachment> builder)
    {
        base.Configure(builder);

        builder.ToTable("attachments");

        builder.Property(x => x.FileName)
            .HasColumnName("file_name")
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.ContentType)
            .HasColumnName("content_type")
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(x => x.SizeBytes)
            .HasColumnName("size_bytes")
            .IsRequired();

        builder.Property(x => x.Checksum)
            .HasColumnName("checksum")
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(x => x.ChecksumAlgorithm)
            .HasColumnName("checksum_algorithm")
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue("SHA256");

        builder.Property(x => x.Type)
            .HasColumnName("attachment_type")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(x => x.ProcessingState)
            .HasColumnName("processing_state")
            .HasConversion<string>()
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.IsPublic)
            .HasColumnName("is_public")
            .IsRequired();

        builder.Property(x => x.Metadata)
            .HasColumnName("metadata")
            .HasColumnType("jsonb")
            .HasConversion(new MetadataJsonConverter());

        builder.HasIndex(x => x.StorageKey);
        builder.HasIndex(x => x.Checksum);
        builder.HasIndex(x => x.ProcessingState);
        builder.HasIndex(x => x.Type);
    }

    private class MetadataJsonConverter : ValueConverter<AttachmentMetadata?, string>
    {
        public MetadataJsonConverter() : base(
            v => v != null ? JsonSerializer.Serialize(v, v.GetType(), (JsonSerializerOptions)null!) : "{}",
            v => Deserialize(v))
        {
        }

        private static AttachmentMetadata? Deserialize(string json)
        {
            if (string.IsNullOrEmpty(json) || json == "{}") return null;

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("embedUrl", out _)) return EmbedMetadata.FromJson(json);
            if (root.TryGetProperty("url", out _)) return LinkMetadata.FromJson(json);
            if (root.TryGetProperty("width", out _)) return MediaMetaData.FromJson(json);
            if (root.TryGetProperty("extension", out _)) return FileMetadata.FromJson(json);
            return null;
        }
    }
}
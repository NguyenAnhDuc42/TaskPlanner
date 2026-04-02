using Domain.Entities.Support.ValueObject;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;

namespace Infrastructure.Data.Configurations.Support;

public class AttachmentConfiguration : EntityConfiguration<Attachment>
{
    public override void Configure(EntityTypeBuilder<Attachment> builder)
    {
        base.Configure(builder);

        builder.ToTable("attachments");

        // 1. Storage Info
        // Renamed from ContentId to StorageKey to match our Entity
        builder.Property(x => x.StorageKey)
            .HasColumnName("storage_key")
            .HasMaxLength(512);

        builder.Property(x => x.StorageProvider)
            .HasConversion<string>()
            .HasColumnName("storage_provider")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.StoragePath)
            .HasColumnName("storage_path")
            .HasMaxLength(2000);

        // 2. Visible Metadata
        builder.Property(x => x.FileName)
            .HasColumnName("file_name")
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(x => x.ContentType)
            .HasColumnName("content_type")
            .HasMaxLength(256);

        builder.Property(x => x.Type)
            .HasConversion<int>() // Store as SmallInt/Int for performance
            .HasColumnName("attachment_type")
            .IsRequired();

        builder.Property(x => x.SizeBytes)
            .HasColumnName("size_bytes")
            .IsRequired();

        // 3. Integrity
        builder.Property(x => x.Checksum)
            .HasColumnName("checksum")
            .HasMaxLength(256);

        builder.Property(x => x.ChecksumAlgorithm)
            .HasColumnName("checksum_algorithm")
            .HasMaxLength(64);

        // 4. Lifecycle & State
        builder.Property(x => x.ProcessingState)
            .HasConversion<string>()
            .HasColumnName("processing_state")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.IsPublic)
            .HasColumnName("is_public")
            .IsRequired();

        // 5. The Polymorphic Metadata (JSONB)
        // This maps the Metadata Value Object to the JSON column in Postgres
        builder.Property(x => x.Metadata)
            .HasColumnName("metadata")
            .HasColumnType("jsonb")
            .HasConversion(new MetadataJsonConverter());

        // 6. Indexes
        builder.HasIndex(x => x.StorageKey); // Removed Unique because multiple links can point to one key
        builder.HasIndex(x => x.Checksum);   // Critical for the de-duplication check
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

            // Easiest Trick: Look for a unique property in the JSON to determine the type
            // This avoids needing the 'Type' column from the DB row.
            if (root.TryGetProperty("embedUrl", out _)) return EmbedMetadata.FromJson(json);
            if (root.TryGetProperty("url", out _)) return LinkMetadata.FromJson(json);
            if (root.TryGetProperty("width", out _)) return MediaMetaData.FromJson(json);
            if (root.TryGetProperty("extension", out _)) return FileMetadata.FromJson(json);
            return null;
        }
    }
}
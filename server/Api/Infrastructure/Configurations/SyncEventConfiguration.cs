using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api;

public class SyncEventConfiguration : IEntityTypeConfiguration<SyncEvent>
{
    public void Configure(EntityTypeBuilder<SyncEvent> builder)
    {
        builder.ToTable("sync_events");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.ProjectWorkspaceId)
            .HasColumnName("project_workspace_id")
            .IsRequired();

        builder.Property(x => x.EntityType)
            .HasColumnName("entity_type")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(x => x.EntityId)
            .HasColumnName("entity_id")
            .IsRequired();

        builder.Property(x => x.Action)
            .HasColumnName("action")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(x => x.Payload)
            .HasColumnName("payload")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(x => x.ClientTraceId)
            .HasColumnName("client_trace_id")
            .IsRequired();

        builder.Property(x => x.AuthorUserId)
            .HasColumnName("author_user_id")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(x => x.IsPublished)
            .HasColumnName("is_published")
            .IsRequired();

        builder.HasOne<ProjectWorkspace>()
            .WithMany()
            .HasForeignKey(x => x.ProjectWorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        // Delta query: WHERE ProjectWorkspaceId = X AND Id > since ORDER BY Id
        builder.HasIndex(x => new { x.ProjectWorkspaceId, x.Id });

        // Background publish worker: WHERE IsPublished = false
        builder.HasIndex(x => x.IsPublished);
    }
}

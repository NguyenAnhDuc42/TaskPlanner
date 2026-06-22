using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;
namespace Application;


public class FavoriteConfiguration : TenantEntityConfiguration<Favorite>
{
    public override void Configure(EntityTypeBuilder<Favorite> builder)
    {
        base.Configure(builder);

        builder.ToTable("favorites");
        builder.Property(x => x.WorkspaceMemberId)
            .HasColumnName("workspace_member_id")
            .IsRequired();

        builder.Property(x => x.EntityId)
            .HasColumnName("entity_id")
            .IsRequired();
        builder.Property(x => x.EntityLayerType)
            .HasColumnName("entity_layer_type")
            .IsRequired();
            
        builder.Property(x => x.OrderKey)
            .HasColumnName("order_key")
            .IsRequired()
            .HasMaxLength(255);

        builder.HasIndex(x => x.WorkspaceMemberId);
        builder.HasIndex(x => new
        {
            x.WorkspaceMemberId,
            x.EntityLayerType,
            x.EntityId
        })
        .IsUnique();

        // Cursor pagination: favorites for a member in a workspace
        builder.HasIndex(x => new { x.WorkspaceMemberId, x.ProjectWorkspaceId, x.OrderKey, x.Id })
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("IX_favorites_member_workspace_order_key");
    }
}
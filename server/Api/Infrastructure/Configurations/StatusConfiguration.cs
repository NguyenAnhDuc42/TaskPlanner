using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Api;

public class StatusConfiguration : TenantEntityConfiguration<Status>
{
    public override void Configure(EntityTypeBuilder<Status> builder)
    {
        base.Configure(builder);

        builder.ToTable("statuses");

        builder.Property(s => s.ProjectSpaceId)
            .HasColumnName("project_space_id")
            .IsRequired();

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Color)
            .HasColumnName("color")
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.Category)
            .HasColumnName("category")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(s => s.OrderKey)
            .HasColumnName("order_key")
            .IsRequired();

        builder.HasIndex(s => new { s.ProjectSpaceId, s.OrderKey });
        builder.HasIndex(s => s.ProjectSpaceId);

        builder.HasOne<ProjectSpace>()
            .WithMany()
            .HasForeignKey(s => s.ProjectSpaceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

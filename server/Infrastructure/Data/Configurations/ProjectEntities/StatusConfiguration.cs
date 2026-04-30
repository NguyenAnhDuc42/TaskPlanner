using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities;
using Domain.Common;

namespace Infrastructure.Data.Configurations;

public class StatusConfiguration : TenantEntityConfiguration<Status>
{
    public override void Configure(EntityTypeBuilder<Status> builder)
    {
        base.Configure(builder);

        builder.ToTable("statuses");

        builder.Property(s => s.WorkflowId)
            .HasColumnName("workflow_id")
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

        builder.HasIndex(s => new { s.WorkflowId, s.OrderKey });

        // Foreign Keys
        builder.HasOne<Workflow>()
            .WithMany(w => w.Statuses)
            .HasForeignKey(x => x.WorkflowId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.WorkflowId);
    }
}

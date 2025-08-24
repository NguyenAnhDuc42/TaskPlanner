using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities.Support;

namespace Infrastructure.Data.Configurations.Support;

public class ChecklistItemConfiguration : IEntityTypeConfiguration<ChecklistItem>
{
    public void Configure(EntityTypeBuilder<ChecklistItem> builder)
    {
        builder.ToTable("ChecklistItems");

        builder.HasKey(cli => cli.Id);

        // Match domain model: ChecklistItem has Text, IsCompleted, AssigneeId, OrderIndex
        builder.Property(cli => cli.Text)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(cli => cli.IsCompleted)
            .IsRequired();

        builder.Property(cli => cli.AssigneeId)
            .IsRequired(false);

        builder.Property(cli => cli.OrderIndex)
            .IsRequired();
    }
}

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

        builder.Property(cli => cli.Description)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(cli => cli.IsCompleted)
            .IsRequired();

        builder.Property(cli => cli.ChecklistId)
            .IsRequired();

        // Configure common Entity properties
        builder.Property(e => e.Version)
            .IsRowVersion();

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .IsRequired();
    }
}

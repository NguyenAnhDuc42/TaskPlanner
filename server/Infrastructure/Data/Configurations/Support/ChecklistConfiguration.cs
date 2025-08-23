using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities.Support;

namespace Infrastructure.Data.Configurations.Support;

public class ChecklistConfiguration : IEntityTypeConfiguration<Checklist>
{
    public void Configure(EntityTypeBuilder<Checklist> builder)
    {
        builder.ToTable("Checklists");

        builder.HasKey(cl => cl.Id);

        builder.Property(cl => cl.Title)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(cl => cl.ProjectTaskId)
            .IsRequired();

        // Configure relationships
        builder.HasMany(cl => cl.Items)
            .WithOne()
            .HasForeignKey(cli => cli.ChecklistId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade); // Checklist items are deleted with checklist

        // Configure common Entity properties
        builder.Property(e => e.Version)
            .IsRowVersion();

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .IsRequired();

        // Ignore domain events collection as it's not persisted
        builder.Ignore(e => e.DomainEvents);
    }
}

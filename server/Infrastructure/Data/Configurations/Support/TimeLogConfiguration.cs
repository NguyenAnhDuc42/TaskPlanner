using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities.Support;

namespace Infrastructure.Data.Configurations.Support;

public class TimeLogConfiguration : IEntityTypeConfiguration<TimeLog>
{
    public void Configure(EntityTypeBuilder<TimeLog> builder)
    {
        builder.ToTable("TimeLogs");

        builder.HasKey(tl => tl.Id);

        builder.Property(tl => tl.TimeSpent)
            .IsRequired();

        builder.Property(tl => tl.UserId)
            .IsRequired();

        builder.Property(tl => tl.ProjectTaskId)
            .IsRequired();

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

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities.Relationship;

namespace Infrastructure.Data.Configurations.Relationship;

public class UserProjectSpaceConfiguration : IEntityTypeConfiguration<UserProjectSpace>
{
    public void Configure(EntityTypeBuilder<UserProjectSpace> builder)
    {
        builder.ToTable("UserProjectSpaces");

        // Composite primary key
        builder.HasKey(ups => new { ups.UserId, ups.ProjectSpaceId });

        // Configure relationships
        builder.HasOne<Domain.Entities.User>(ups => ups.User)
            .WithMany()
            .HasForeignKey(ups => ups.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Domain.Entities.ProjectWorkspace.ProjectSpace>(ups => ups.ProjectSpace)
            .WithMany(ps => ps.Members)
            .HasForeignKey(ups => ups.ProjectSpaceId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure common Entity properties
        builder.Property(e => e.Version)
            .IsRowVersion();

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .IsRequired();
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities.Relationship;
using Domain.Entities.ProjectEntities; // Add this using statement for ProjectSpace

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

        builder.HasOne<ProjectSpace>(ups => ups.ProjectSpace) // Corrected namespace
            .WithMany(ps => ps.Members)
            .HasForeignKey(ups => ups.ProjectSpaceId)
            .OnDelete(DeleteBehavior.Cascade);

    // UserProjectSpace is a relationship POCO and does not have Version/CreatedAt/UpdatedAt
    }
}

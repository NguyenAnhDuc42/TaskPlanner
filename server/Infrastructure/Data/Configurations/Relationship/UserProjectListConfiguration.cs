using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities.Relationship;
using Domain.Entities.ProjectEntities; // Add this using statement for ProjectList

namespace Infrastructure.Data.Configurations.Relationship;

public class UserProjectListConfiguration : IEntityTypeConfiguration<UserProjectList>
{
    public void Configure(EntityTypeBuilder<UserProjectList> builder)
    {
        builder.ToTable("UserProjectLists");

        // Composite primary key
        builder.HasKey(upl => new { upl.UserId, upl.ProjectListId });

        // Configure relationships
        builder.HasOne<Domain.Entities.User>(upl => upl.User)
            .WithMany()
            .HasForeignKey(upl => upl.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ProjectList>(upl => upl.ProjectList) // Corrected namespace
            .WithMany(pl => pl.Members)
            .HasForeignKey(upl => upl.ProjectListId)
            .OnDelete(DeleteBehavior.Cascade);

    // UserProjectList is a relationship POCO and does not have Version/CreatedAt/UpdatedAt
    }
}

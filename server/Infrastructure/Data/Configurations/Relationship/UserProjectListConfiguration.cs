using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities.Relationship;

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

        builder.HasOne<Domain.Entities.ProjectWorkspace.ProjectList>(upl => upl.ProjectList)
            .WithMany(pl => pl.Members)
            .HasForeignKey(upl => upl.ProjectListId)
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

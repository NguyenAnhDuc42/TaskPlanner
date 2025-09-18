using Domain.Entities.Relationship;
using Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class UserAccessLayerConfiguration : EntityConfiguration<UserAccessLayer>
{
    public override void Configure(EntityTypeBuilder<UserAccessLayer> builder)
    {
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.EntityId).IsRequired();
        builder.Property(x => x.EntityType).IsRequired();
        builder.Property(x => x.AccessLevel).IsRequired();

        builder.HasIndex(x => new { x.UserId, x.EntityId, x.EntityType })
            .IsUnique();
    }
}

using System;
using Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public abstract class CompositeConfiguration<TEntity> : IEntityTypeConfiguration<TEntity> where TEntity : Composite
{
    public virtual void Configure(EntityTypeBuilder<TEntity> builder)
    {
        // Common Composite configuration
        builder.Property(x => x.CreatedAt).IsRequired().HasColumnName("created_at").HasConversion<DateTimeOffset>();

        builder.Property(x => x.UpdatedAt).IsRequired().HasColumnName("updated_at").HasConversion<DateTimeOffset>();

        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasConversion<DateTimeOffset>();
        builder.Property(x => x.CreatorId).HasColumnName("creator_id");  
        
        builder.HasQueryFilter(x => x.DeletedAt == null);

        builder.HasIndex(x => x.CreatorId);  
        builder.Ignore(x => x.DomainEvents);


    }

}

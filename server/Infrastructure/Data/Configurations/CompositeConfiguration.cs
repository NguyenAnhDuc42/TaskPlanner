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
        builder.Property(x => x.Version)
            .IsRowVersion(); // concurrency token

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired();

        // _domainEvents and DomainEvents are ignored
        builder.Ignore(x => x.DomainEvents);


    }

}

using System;
using Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public abstract class EntityConfiguration<TEntity> : IEntityTypeConfiguration<TEntity> where TEntity : Entity
{
    public virtual void Configure(EntityTypeBuilder<TEntity> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).ValueGeneratedNever().HasColumnName("id"); // GUIDs generated in domain

        builder.Property(x => x.CreatedAt).IsRequired().HasColumnName("created_at").HasConversion<DateTimeOffset>();

        builder.Property(x => x.UpdatedAt).IsRequired().HasColumnName("updated_at").HasConversion<DateTimeOffset>();

        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasConversion<DateTimeOffset>();

        builder.Property(x => x.CreatorId).HasColumnName("creator_id");

        builder.HasIndex(x => x.CreatorId);
        builder.Ignore(x => x.DomainEvents);
    }
}
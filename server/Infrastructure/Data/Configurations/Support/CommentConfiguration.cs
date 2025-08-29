using System;
using Domain.Entities;
using Domain.Entities.ProjectEntities;
using Domain.Entities.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations.Support;

public class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        builder.ToTable("Comments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Content)
               .HasMaxLength(2000)
               .IsRequired();

        builder.HasOne<ProjectTask>()
               .WithMany(t => t.Comments)
               .HasForeignKey(x => x.ProjectTaskId);

        builder.HasOne<User>()
               .WithMany()
               .HasForeignKey(x => x.AuthorId);

        builder.HasOne<Comment>()
               .WithMany()
               .HasForeignKey(x => x.ParentCommentId)
               .OnDelete(DeleteBehavior.Restrict); // prevent cascade loops
    }
}

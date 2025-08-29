using System;
using Domain.Entities.Relationship;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations.Relationship;

public class ProjectTaskTagConfiguration : IEntityTypeConfiguration<ProjectTaskTag>
    {
        public void Configure(EntityTypeBuilder<ProjectTaskTag> builder)
        {
            builder.ToTable("ProjectTaskTags");

            builder.HasKey(x => new { x.ProjectTaskId, x.TagId });

            builder.HasOne(x => x.ProjectTask)
                   .WithMany() 
                   .HasForeignKey(x => x.ProjectTaskId);

            builder.HasOne(x => x.Tag)
                   .WithMany() 
                   .HasForeignKey(x => x.TagId);

            builder.Property(x => x.CreatedAt);
        }
    }

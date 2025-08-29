using System;
using Domain.Entities.ProjectEntities;
using Domain.Entities.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations.Support;

public class ChecklistConfiguration : IEntityTypeConfiguration<Checklist>
{
    public void Configure(EntityTypeBuilder<Checklist> builder)
    {
        builder.ToTable("Checklists");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title)
               .HasMaxLength(255)
               .IsRequired();

        builder.HasOne<ProjectTask>()
               .WithMany(t => t.Checklists)
               .HasForeignKey(x => x.ProjectTaskId);
    }
}
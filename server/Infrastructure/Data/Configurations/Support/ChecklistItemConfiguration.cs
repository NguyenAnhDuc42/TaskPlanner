using System;
using Domain.Entities.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations.Support;

public class ChecklistItemConfiguration : IEntityTypeConfiguration<ChecklistItem>
{
    public void Configure(EntityTypeBuilder<ChecklistItem> builder)
    {
        builder.ToTable("ChecklistItems");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Text)
               .HasMaxLength(500)
               .IsRequired();

        builder.HasOne<Checklist>()
               .WithMany() // no Items navigation anymore
               .HasForeignKey(x => x.ChecklistId);
    }
}

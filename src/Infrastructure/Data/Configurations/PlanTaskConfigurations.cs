using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using src.Domain.Entities.WorkspaceEntity;

namespace src.Infrastructure.Data.Configurations;

public class PlanTaskConfigurations : IEntityTypeConfiguration<PlanTask>
{
    public void Configure(EntityTypeBuilder<PlanTask> builder)
    {

        builder.HasKey(t => t.Id);
        builder.Property(t => t.WorkspaceId);
        builder.Property(t => t.SpaceId).IsRequired();
        builder.Property(t => t.FolderId).IsRequired(false);
        builder.Property(t => t.ListId).IsRequired();
        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(200);
        builder.Property(t => t.Description)
            .IsRequired(false)
            .HasMaxLength(2000);
        builder.Property(t => t.Priority);
        builder.Property(t => t.DueDate).IsRequired(false);
        builder.Property(t => t.StartDate).IsRequired(false);
        builder.Property(t => t.TimeEstimate).IsRequired(false);
        builder.Property(t => t.TimeSpent).IsRequired(false);
        builder.Property(t => t.OrderIndex);
        builder.Property(t => t.IsArchived);
        builder.Property(t => t.IsPrivate);
        builder.Property(t => t.CreatorId).IsRequired();
        builder.Property(t => t.StatusId);

        builder.HasIndex(t => t.WorkspaceId);
        builder.HasIndex(t => t.SpaceId);
        builder.HasIndex(t => t.FolderId);
        builder.HasIndex(t => t.ListId);
        builder.HasIndex(t => t.CreatorId);
        builder.HasIndex(t => t.StatusId);

        builder.HasMany(t => t.Asignees)
        .WithOne()
        .HasForeignKey(ut => ut.TaskId)
        .OnDelete(DeleteBehavior.Cascade);
    }
} 
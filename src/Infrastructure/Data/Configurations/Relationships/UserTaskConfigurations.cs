using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using src.Domain.Entities.UserEntity;
using src.Domain.Entities.WorkspaceEntity;
using src.Domain.Entities.WorkspaceEntity.Relationships;

namespace src.Infrastructure.Data.Configurations.Relationships;

public class UserTaskConfigurations : IEntityTypeConfiguration<UserTask>
{
    public void Configure(EntityTypeBuilder<UserTask> builder)
    {
        builder.ToTable("UserTasks");
        builder.HasKey(uw => new { uw.UserId, uw.TaskId });
        builder.HasOne<User>()
            .WithMany(u => u.Tasks)
            .HasForeignKey(uw => uw.UserId);
        builder.HasOne<PlanTask>()
            .WithMany(t => t.Asignees)
            .HasForeignKey(uw => uw.TaskId);

        builder.HasIndex(uw => uw.UserId);
        builder.HasIndex(uw => uw.TaskId);
    }
}

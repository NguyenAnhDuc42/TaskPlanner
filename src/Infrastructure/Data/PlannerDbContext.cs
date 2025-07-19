using System;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using src.Domain.Entities.SessionEntity;
using src.Domain.Entities.UserEntity;
using src.Domain.Entities.WorkspaceEntity;
using src.Domain.Entities.WorkspaceEntity.Relationships;
using src.Domain.Enums;

namespace src.Infrastructure.Data;

public class PlannerDbContext : DbContext
{
    public PlannerDbContext(DbContextOptions<PlannerDbContext> options) : base(options) { }

    public virtual DbSet<User> Users { get; set; }
    public virtual DbSet<Session> Sessions { get; set; }
    public virtual DbSet<Workspace> Workspaces { get; set; }
    public virtual DbSet<Space> Spaces { get; set; }
    public virtual DbSet<PlanList> Lists { get; set; }
    public virtual DbSet<PlanFolder> Folders { get; set; }
    public virtual DbSet<PlanTask> Tasks { get; set; }


    public virtual DbSet<UserWorkspace> UserWorkspaces { get; set; }
    public virtual DbSet<UserSpace> UserSpaces { get; set; }
    public virtual DbSet<UserList> UserLists { get; set; }
    public virtual DbSet<UserFolder> UserFolders { get; set; }
    public virtual DbSet<UserTask> UserTasks { get; set; }



    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresEnum<Role>();
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }
}

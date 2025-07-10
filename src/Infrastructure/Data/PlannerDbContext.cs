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

    public DbSet<User> Users { get; set; }
    public DbSet<Session> Sessions { get; set; }
    public DbSet<Workspace> Workspaces { get; set; }
    public DbSet<Space> Spaces { get; set; }
    public DbSet<PlanList> Lists { get; set; }
    public DbSet<PlanFolder> Folders { get; set; }
    public DbSet<PlanTask> Tasks { get; set; }


    public DbSet<UserWorkspace> UserWorkspaces { get; set; }
    public DbSet<UserSpace> UserSpaces { get; set; }
    public DbSet<UserList> UserLists { get; set; }
    public DbSet<UserFolder> UserFolders { get; set; }
    public DbSet<UserTask> UserTasks { get; set; }



    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresEnum<Role>();
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }
}

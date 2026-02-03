using Microsoft.EntityFrameworkCore;
using System.Reflection; 
using Domain.Entities;
using Domain.Entities.Relationship;
using Domain.Entities.Support;
using Domain.Entities.ProjectEntities;
using Domain.Common;
using Domain.Entities.Support.Workspace;
using Domain.Entities.Support.Widget;
namespace Infrastructure.Data;

public class TaskPlanDbContext : DbContext
{
    public TaskPlanDbContext(DbContextOptions<TaskPlanDbContext> options) : base(options)
    {
    }

    // DbSet properties for entities
    public DbSet<User> Users { get; set; }
    public DbSet<Session> Sessions { get; set; }

    public DbSet<ProjectWorkspace> ProjectWorkspaces { get; set; }
    public DbSet<ProjectSpace> ProjectSpaces { get; set; }
    public DbSet<ProjectFolder> ProjectFolders { get; set; }
    public DbSet<ProjectList> ProjectLists { get; set; }
    public DbSet<ProjectTask> ProjectTasks { get; set; }

    // Relationship Entities
    public DbSet<WorkspaceMember> WorkspaceMembers { get; set; }
    public DbSet<EntityAccess> EntityAccesses { get; set; }
    public DbSet<EntityMember> EntityMembers { get; set; }
    public DbSet<ChatRoomMember> ChatRoomMembers { get; set; }
    public DbSet<TaskAssignment> TaskAssignments { get; set; } 

    // Support Entities
    public DbSet<Attachment> Attachments { get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<Status> Statuses { get; set; }
    public DbSet<ChatRoom> ChatRooms { get; set; }
    public DbSet<ChatMessage> ChatMessages { get; set; }

    public DbSet<Widget> Widgets { get; set; }
    public DbSet<Dashboard> Dashboards { get; set; }
    
    public DbSet<OutboxMessage> OutboxMessages { get; set; }
    





    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        base.OnModelCreating(modelBuilder);
    }
}

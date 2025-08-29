using Microsoft.EntityFrameworkCore;
using System.Reflection; // Needed for Assembly.GetExecutingAssembly()

// Import all Domain.Entities namespaces
using Domain.Entities;

using Domain.Entities.Relationship;
using Domain.Entities.Support;
using Domain.Entities.ProjectEntities;
using Domain.Common;

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
    public DbSet<UserProjectWorkspace> UserProjectWorkspaces { get; set; }
    public DbSet<UserProjectSpace> UserProjectSpaces { get; set; }
    public DbSet<UserProjectFolder> UserProjectFolders { get; set; }
    public DbSet<UserProjectList> UserProjectLists { get; set; }
    public DbSet<UserProjectTask> UserProjectTasks { get; set; }

    // Support Entities
    public DbSet<Attachment> Attachments { get; set; }
    public DbSet<Checklist> Checklists { get; set; }
    public DbSet<ChecklistItem> ChecklistItems { get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<Status> Statuses { get; set; }
    public DbSet<Tag> Tags { get; set; }
    public DbSet<TimeLog> TimeLogs { get; set; }
    public DbSet<ProjectTaskTag> ProjectTaskTags { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

        // Apply configurations from the assembly where this DbContext resides
        // This will pick up all IEntityTypeConfiguration implementations in the same assembly
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

         foreach (var entityType in modelBuilder.Model.GetEntityTypes())
    {
        if (typeof(Entity).IsAssignableFrom(entityType.ClrType))
        {
            modelBuilder.Entity(entityType.ClrType).Property<byte[]>("Version")
                .IsRowVersion()
                .IsConcurrencyToken();

            modelBuilder.Entity(entityType.ClrType).Property<DateTime>("CreatedAt").IsRequired();
            modelBuilder.Entity(entityType.ClrType).Property<DateTime>("UpdatedAt").IsRequired();
        }
    }
        base.OnModelCreating(modelBuilder);
    }
}

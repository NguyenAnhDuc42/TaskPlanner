using Microsoft.EntityFrameworkCore;
using System.Reflection; // Needed for Assembly.GetExecutingAssembly()

// Import all Domain.Entities namespaces
using Domain.Entities;

using Domain.Entities.Relationship;
using Domain.Entities.Support;
using Domain.Entities.ProjectEntities;

namespace Infrastructure.Data;

public class TaskPlanDbContext : DbContext // Corrected name
{
    public TaskPlanDbContext(DbContextOptions<TaskPlanDbContext> options) : base(options) // Corrected name
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
    public DbSet<ProjectTaskWatcher> ProjectTaskWatchers { get; set; }

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
        // Configure composite keys for relationship entities
        modelBuilder.Entity<UserProjectWorkspace>().HasKey(upw => new { upw.UserId, upw.ProjectWorkspaceId });
                modelBuilder.Entity<UserProjectSpace>().HasKey(ups => new { ups.UserId, ups.ProjectSpaceId });
        modelBuilder.Entity<UserProjectFolder>().HasKey(upf => new { upf.UserId, upf.ProjectFolderId });
        modelBuilder.Entity<UserProjectList>().HasKey(upl => new { upl.UserId, upl.ProjectListId });
        modelBuilder.Entity<UserProjectTask>().HasKey(upt => new { upt.UserId, upt.ProjectTaskId });
        modelBuilder.Entity<ProjectTaskWatcher>().HasKey(ptw => new { ptw.ProjectTaskId, ptw.UserId });

        // Configure ProjectTaskTag as a join entity for many-to-many relationship
        modelBuilder.Entity<ProjectTaskTag>()
            .HasKey(ptt => new { ptt.ProjectTaskId, ptt.TagId });

        modelBuilder.Entity<ProjectTaskTag>()
            .HasOne(ptt => ptt.ProjectTask)
            .WithMany() // No direct collection on ProjectTask for ProjectTaskTag
            .HasForeignKey(ptt => ptt.ProjectTaskId);

        modelBuilder.Entity<ProjectTaskTag>()
            .HasOne(ptt => ptt.Tag)
            .WithMany() // No direct collection on Tag for ProjectTaskTag
            .HasForeignKey(ptt => ptt.TagId);

        // Apply configurations from the assembly where this DbContext resides
        // This will pick up all IEntityTypeConfiguration implementations in the same assembly
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }
}

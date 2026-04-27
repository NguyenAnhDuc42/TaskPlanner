using Microsoft.EntityFrameworkCore;
using System.Reflection; 
using Domain.Entities;
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
    public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }

    public DbSet<ProjectWorkspace> ProjectWorkspaces { get; set; }
    public DbSet<ProjectSpace> ProjectSpaces { get; set; }
    public DbSet<ProjectFolder> ProjectFolders { get; set; }
    public DbSet<ProjectTask> ProjectTasks { get; set; }
    public DbSet<Workflow> Workflows { get; set; }
    public DbSet<ViewDefinition> ViewDefinitions { get; set; }
    public DbSet<Document> Documents { get; set; }

    // Relationship Entities
    public DbSet<WorkspaceMember> WorkspaceMembers { get; set; }
    public DbSet<EntityAccess> EntityAccesses { get; set; }
    public DbSet<TaskAssignment> TaskAssignments { get; set; } 
    public DbSet<EntityAssetLink> EntityAssetLinks { get; set; }
    public DbSet<AttachmentLink> AttachmentLinks { get; set; }

    // Support Entities
    public DbSet<Attachment> Attachments { get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<Status> Statuses { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Globally map all Enums to strings
        configurationBuilder.Properties<Enum>().HaveConversion<string>();
        
        // Globally map all DateTimeOffset to UTC for PostgreSQL
        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<DateTimeOffset>();
    }
}

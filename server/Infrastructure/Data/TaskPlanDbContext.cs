using Microsoft.EntityFrameworkCore;
using System.Reflection; 
using Domain.Entities;
using Domain.Common;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Data;

public class TaskPlanDbContext : DbContext
{
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public TaskPlanDbContext(DbContextOptions<TaskPlanDbContext> options, IHttpContextAccessor? httpContextAccessor = null) : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    // Dynamic Tenant ID from current HTTP Request
    public Guid? CurrentTenantId => _httpContextAccessor?.HttpContext?.Items["WorkspaceId"] as Guid?;

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
    public DbSet<DocumentBlock> DocumentBlocks { get; set; }

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

        // Global Query Filters for RLS (Row-Level Security & Tenant Isolation)
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ITenanted).IsAssignableFrom(entityType.ClrType))
            {
                var method = typeof(TaskPlanDbContext)
                    .GetMethod(nameof(ApplyTenantFilter), BindingFlags.NonPublic | BindingFlags.Instance)
                    ?.MakeGenericMethod(entityType.ClrType);

                method?.Invoke(this, new object[] { modelBuilder });
            }
        }

        base.OnModelCreating(modelBuilder);
    }

    private void ApplyTenantFilter<T>(ModelBuilder modelBuilder) where T : class, ITenanted
    {
        // Enforce that a query only returns data for the active WorkspaceId. 
        // If CurrentTenantId is null (e.g. background jobs, migrations), the filter is bypassed.
        modelBuilder.Entity<T>().HasQueryFilter(e => CurrentTenantId == null || e.ProjectWorkspaceId == CurrentTenantId);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Globally map all Enums to strings
        configurationBuilder.Properties<Enum>().HaveConversion<string>();
        
        // Globally map all DateTimeOffset to UTC for PostgreSQL
        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<DateTimeOffset>();
    }
}

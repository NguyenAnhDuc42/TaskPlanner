using Microsoft.EntityFrameworkCore;
using System.Reflection; 
using Microsoft.AspNetCore.Http;

namespace Application;

public class TaskPlanDbContext : DbContext
{
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public TaskPlanDbContext(DbContextOptions<TaskPlanDbContext> options, IHttpContextAccessor? httpContextAccessor = null) : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? CurrentTenantId => _httpContextAccessor?.HttpContext?.Items["WorkspaceId"] as Guid?;

    // DbSet properties for entities
    public DbSet<User> Users { get; set; }
    public DbSet<Session> Sessions { get; set; }
    public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }

    public DbSet<ProjectWorkspace> ProjectWorkspaces { get; set; }
    public DbSet<ProjectSpace> ProjectSpaces { get; set; }
    public DbSet<ProjectFolder> ProjectFolders { get; set; }
    public DbSet<ProjectTask> ProjectTasks { get; set; }
    public DbSet<Document> Documents { get; set; }
    public DbSet<DocumentBlock> DocumentBlocks { get; set; }

    // Relationship Entities
    public DbSet<WorkspaceMember> WorkspaceMembers { get; set; }
    public DbSet<EntityAccess> EntityAccesses { get; set; }
    public DbSet<TaskAssignment> TaskAssignments { get; set; } 
    public DbSet<EntityAssetLink> EntityAssetLinks { get; set; }
    public DbSet<AttachmentLink> AttachmentLinks { get; set; }

    // Support Entities
    public DbSet<Favorite> Favorites { get; set; }
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
        modelBuilder.Entity<T>().HasQueryFilter(e => CurrentTenantId == null || e.ProjectWorkspaceId == CurrentTenantId);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<Enum>().HaveConversion<string>();
        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<DateTimeOffset>();
    }
}



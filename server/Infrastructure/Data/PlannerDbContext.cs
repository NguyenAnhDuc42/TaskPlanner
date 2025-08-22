using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class PlannerDbContext : DbContext
{
    public PlannerDbContext(DbContextOptions<PlannerDbContext> options) : base(options)
    {
    }

    // DbSet properties for entities will go here
    // e.g., public DbSet<ProjectWorkspace> Workspaces { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply configurations from the assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PlannerDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}

using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Data.Configurations;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<TaskPlanDbContext>
{
    public TaskPlanDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TaskPlanDbContext>();

        // Prefer explicit env var for CLI usage
        var conn = Environment.GetEnvironmentVariable("TASKPLAN_CONNECTION_STRING");
        if (!string.IsNullOrWhiteSpace(conn))
        {
            optionsBuilder.UseNpgsql(conn, npg =>
                npg.MigrationsAssembly(typeof(TaskPlanDbContext).Assembly.FullName));
            return new TaskPlanDbContext(optionsBuilder.Options);
        }

        // Fallback: load config files from the project's output folder
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory) // important for dotnet ef
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        conn = config.GetConnectionString("DefaultConnection")
               ?? throw new InvalidOperationException("Connection string not found for design-time DbContext creation.");

        optionsBuilder.UseNpgsql(conn, npg =>
            npg.MigrationsAssembly(typeof(TaskPlanDbContext).Assembly.FullName));

        return new TaskPlanDbContext(optionsBuilder.Options);
    }
}

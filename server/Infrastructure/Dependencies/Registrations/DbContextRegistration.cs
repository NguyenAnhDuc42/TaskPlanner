using System;
using System.Data;
using Dapper;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Dependencies.Registrations;

public static class DbContextRegistration
{
    public static IServiceCollection AddDbContextInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        // Enable Dapper to match snake_case database columns to PascalCase C# properties
        DefaultTypeMap.MatchNamesWithUnderscores = true;

        if (!services.Any(d =>
         d.ServiceType == typeof(DbContextOptions<TaskPlanDbContext>) ||
         d.ServiceType == typeof(TaskPlanDbContext)))
        {
            services.AddDbContext<TaskPlanDbContext>(options =>
                options.UseNpgsql(connectionString));
        }

        // 2. Always register IDbConnection (safe for both cases)
        // Using a separate connection instance for Dapper to avoid ObjectDisposedException
        // when sharing the connection with DbContext.
        services.AddScoped<IDbConnection>(_ => new Npgsql.NpgsqlConnection(connectionString));

        return services;
    }
}
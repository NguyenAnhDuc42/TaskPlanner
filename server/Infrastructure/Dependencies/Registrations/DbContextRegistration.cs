using System;
using System.Data;
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
        if (!services.Any(d =>
         d.ServiceType == typeof(DbContextOptions<TaskPlanDbContext>) ||
         d.ServiceType == typeof(TaskPlanDbContext)))
        {
            services.AddDbContext<TaskPlanDbContext>(options =>
                options.UseNpgsql(connectionString));
        }

        // 2. Always register IDbConnection (safe for both cases)
        services.AddScoped<IDbConnection>(sp =>
        {
            var db = sp.GetRequiredService<TaskPlanDbContext>();
            return db.Database.GetDbConnection();
        });

        return services;
    }
}
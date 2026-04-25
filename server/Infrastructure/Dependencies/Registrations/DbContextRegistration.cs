using System;
using System.Data;
using Dapper;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Dependencies.Registrations;

public static class DbContextRegistration
{
    public static IServiceCollection AddDbContextInfrastructure(this IServiceCollection services, string connectionString)
    {
      
        DefaultTypeMap.MatchNamesWithUnderscores = true;

        if (!services.Any(d =>
         d.ServiceType == typeof(DbContextOptions<TaskPlanDbContext>) ||
         d.ServiceType == typeof(TaskPlanDbContext)))
        {
            services.AddDbContext<TaskPlanDbContext>(options => options.UseNpgsql(connectionString));
        }

        services.AddScoped<IDbConnection>(_ => new Npgsql.NpgsqlConnection(connectionString));

        return services;
    }
}
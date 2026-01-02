using System;
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
        // If Aspire already registered the DbContext, we don't want to overwrite it
        if (services.Any(x => x.ServiceType == typeof(TaskPlanDbContext)))
        {
            return services;
        }

        services.AddDbContext<TaskPlanDbContext>(options =>
            options.UseNpgsql(connectionString));

        return services;
    }
}
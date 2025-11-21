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
        services.AddDbContext<TaskPlanDbContext>(options =>
            options.UseNpgsql(connectionString));

        return services;
    }
}
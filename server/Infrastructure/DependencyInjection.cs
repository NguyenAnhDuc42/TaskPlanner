using System;
using Application.Interfaces.Repositories;
using Infrastructure.Data;
using Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, string connectionString)
    {
        // Register DbContext
        services.AddDbContext<TaskPlanDbContext>(options =>
            options.UseNpgsql(connectionString));


        services.Scan(scan => scan
             .FromAssembliesOf(typeof(UnitOfWork), typeof(BaseRepository<>))
             .AddClasses(c => c.InNamespaces("Infrastructure.Data.Repositories"))
                 .AsImplementedInterfaces()
                 .WithScopedLifetime()
         );

        services.AddScoped<IUnitOfWork, UnitOfWork>();




        return services;
    }
}
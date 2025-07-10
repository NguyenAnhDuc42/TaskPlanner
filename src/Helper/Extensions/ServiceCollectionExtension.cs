using System;
using Microsoft.EntityFrameworkCore;
using src.Infrastructure.Abstractions.IRepositories;
using src.Infrastructure.Abstractions.IServices;
using src.Infrastructure.Data;
using src.Infrastructure.Data.Repositories;
using src.Infrastructure.Services;

namespace src.Helper.Extensions;

public static class ServiceCollectionExtension
{

    public static IServiceCollection ServiceCollection(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<PlannerDbContext>(opt => opt.UseNpgsql(config.GetConnectionString("DefaultConnection") ?? throw new ArgumentException("DefaultConnection is not set in configuration")));

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ISessionRepository, SessionRepository>();



        // Services
        services.AddScoped<IPasswordService, PasswordService>();
 

        return services;
    }

}

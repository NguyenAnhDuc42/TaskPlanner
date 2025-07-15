using System;
using System.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
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
        var connectionString = config.GetConnectionString("DefaultConnection") ?? throw new ArgumentException("DefaultConnection is not set in configuration");

        services.AddDbContext<PlannerDbContext>(opt => opt.UseNpgsql(connectionString));
        services.AddScoped<IDbConnection>(opt => new NpgsqlConnection(connectionString));

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ISessionRepository, SessionRepository>();



        // Services
        services.AddScoped<IPasswordService, PasswordService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

 

        return services;
    }

}

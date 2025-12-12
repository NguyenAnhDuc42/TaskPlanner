using System;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Interfaces.Services.Permissions;
using Infrastructure.Auth;
using Infrastructure.Data;
using Infrastructure.Services;
using Infrastructure.Services.Permissions;
using Microsoft.Extensions.DependencyInjection;
using server.Application.Interfaces;

namespace Infrastructure.Dependencies.Registrations;

public static class ServicesRegistration
{
    public static IServiceCollection AddServices(this IServiceCollection services)
    {

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<ICookieService, CookieService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<PermissionContextBuilder>();
        services.AddScoped<PermissionDataFetcher>();
        services.AddScoped<IRealtimeService, SignalRRealtimeService>();
        services.AddTransient<IEmailService, EmailService>();
        services.AddTransient<IExternalAuthService, ExternalAuthService>();

        services.AddHttpContextAccessor();


        services.AddSingleton<IPasswordService, PasswordService>();


        return services;
    }
}


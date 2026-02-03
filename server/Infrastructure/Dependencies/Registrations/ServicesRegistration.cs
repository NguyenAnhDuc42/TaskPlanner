using Application.Interfaces;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Interfaces.Services.Permissions;
using Application.Helpers.Permission;
using Infrastructure.Auth;
using Infrastructure.Data;
using Infrastructure.Services;
using Infrastructure.Services.Permissions;
using Microsoft.Extensions.DependencyInjection;
using server.Application.Interfaces;
using System;

namespace Infrastructure.Dependencies.Registrations;

public static class ServicesRegistration
{
    public static IServiceCollection AddServices(this IServiceCollection services)
    {

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<ICookieService, CookieService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IRealtimeService, SignalRRealtimeService>();
        services.AddTransient<IExternalAuthService, ExternalAuthService>();

        // New Permission System
        services.AddScoped<IPermissionProvider, PermissionProvider>();
        services.AddScoped<PermissionResolver>();
        services.AddScoped<IEntityHierarchyProvider, EntityHierarchyProvider>();
        services.AddScoped<IAccessGrantService, AccessGrantService>();

        services.AddHttpContextAccessor();


        services.AddSingleton<IPasswordService, PasswordService>();


        return services;
    }
}


using System;
using Microsoft.AspNetCore.Authorization;
using src.Helper.Middleware;
using src.Helper.Permission;


namespace src.Helper.Extensions;

public static class AuthorizationPolicyExtensions
{
    public static AuthorizationPolicyBuilder RequirePermission(this AuthorizationPolicyBuilder builder, Permission.Permission permission, bool checkCreator = false)
    {
        return builder.AddRequirements(new PermissionRequirement(permission, checkCreator));
    }
    public static IServiceCollection AddPermissionSystem(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddScoped<IAuthorizationHandler, PermissionHandler>(); // Registers the custom permission handler

        services.AddAuthorization(options =>
        {
            // Define authorization policies for various permissions
            options.AddPolicy("ViewTask", policy => policy.RequirePermission(Permission.Permission.ViewTask));
            options.AddPolicy("CreateTask", policy => policy.RequirePermission(Permission.Permission.CreateTask));
            options.AddPolicy("EditTask", policy => policy.RequirePermission(Permission.Permission.EditTask, checkCreator: true));
            options.AddPolicy("DeleteTask", policy => policy.RequirePermission(Permission.Permission.DeleteTask, checkCreator: true));

            options.AddPolicy("ManageUsers", policy => policy.RequirePermission(Permission.Permission.ManageUsers));
        });

        return services;
    }
    
    public static IApplicationBuilder UsePermissionSystem(this IApplicationBuilder app)
    {
        app.UseMiddleware<WorkspaceClaimsMiddleware>();
        return app;
    }
}

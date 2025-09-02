using System;
using System.Text;
using Application.Interfaces.Repositories;
using Infrastructure.Auth;
using Infrastructure.Data;
using Infrastructure.Data.Repositories;
using Infrastructure.Helper;
using Infrastructure.Helper.Configurations;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Scrutor;
using server.Application.Interfaces;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString, IConfiguration config)
    {
        var jwtSettings = config.GetSection("JwtSettings").Get<JwtSettings>() ?? throw new ArgumentException("JwtSettings is not set in configuration");
        // Register DbContext
        services.AddDbContext<TaskPlanDbContext>(options =>
            options.UseNpgsql(connectionString));


        services.Scan(scan => scan
                // anchor to the Infrastructure assembly via marker
                .FromAssemblyOf<InfrastructureAssemblyMarker>()

                // --- Repositories: types ending with "Repository" -> register matching interface I{TypeName}
                .AddClasses(classes => classes.Where(t => t.Name.EndsWith("Repository", StringComparison.Ordinal)))
                    .UsingRegistrationStrategy(RegistrationStrategy.Skip) // skip if interface already registered
                    .AsMatchingInterface() // e.g. FooRepository -> IFooRepository
                    .WithScopedLifetime()

                // --- Services: types ending with "Service" -> register matching interface I{TypeName}
                .AddClasses(classes => classes.Where(t => t.Name.EndsWith("Service", StringComparison.Ordinal)))
                    .UsingRegistrationStrategy(RegistrationStrategy.Skip)
                    .AsMatchingInterface()
                    .WithScopedLifetime()

                // --- Fallback: anything left in infra that implements interfaces => register all implemented interfaces
                .AddClasses(classes => classes.InNamespaces("Infrastructure")
                                           .Where(t => !t.Name.EndsWith("Repository", StringComparison.Ordinal)
                                                     && !t.Name.EndsWith("Service", StringComparison.Ordinal)))
                    .AsImplementedInterfaces()
                    .WithScopedLifetime()
        );


        services.Configure<JwtSettings>(config.GetSection("JwtSettings"));

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(opt =>
            {
                opt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
                    ClockSkew = TimeSpan.Zero
                };

                // Read JWT from cookie if Authorization header is missing
                opt.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        if (string.IsNullOrEmpty(context.Token))
                        {
                            var cookieService = context.HttpContext.RequestServices.GetRequiredService<ICookieService>();
                            var tokens = cookieService.GetAuthTokensFromCookies(context.HttpContext);
                            context.Token = tokens?.AccessToken;
                        }
                        return Task.CompletedTask;
                    }
                };
            });


        services.Configure<CursorEncryptionOptions>(config.GetSection(CursorEncryptionOptions.SectionName));
        services.AddSingleton<CursorHelper>();




        return services;
    }
}
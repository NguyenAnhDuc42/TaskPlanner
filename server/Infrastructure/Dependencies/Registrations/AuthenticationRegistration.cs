using System;
using System.Text;
using Infrastructure.Auth;
using Infrastructure.Auth.Types;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using server.Application.Interfaces;

namespace Infrastructure.Dependencies.Registrations;

public static class AuthenticationRegistration
{
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services, IConfiguration config)
    {
        var jwtSettings = config.GetSection("JwtSettings").Get<JwtSettings>()
            ?? throw new ArgumentException("JwtSettings is not set in configuration");

        services.Configure<JwtSettings>(config.GetSection("JwtSettings"));
        services.Configure<CookieSettings>(config.GetSection("CookieSettings"));

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
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
                    ClockSkew = TimeSpan.Zero
                };

                opt.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        if (string.IsNullOrEmpty(context.Token))
                        {
                            var cookieService = context.HttpContext.RequestServices
                                .GetRequiredService<ICookieService>();
                            var tokens = cookieService.GetAuthTokensFromCookies(context.HttpContext);
                            context.Token = tokens?.AccessToken;
                        }
                        return Task.CompletedTask;
                    }
                };
            });

        return services;
    }
}

using System;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using src.Infrastructure.Abstractions.IServices;
using src.Infrastructure.Services;

namespace src.Helper.Extensions;

public static class IdentityExtension
{
    public static IServiceCollection IdentityService(this IServiceCollection services, IConfiguration config)
    {
        var jwtSetting = config.GetSection("JwtSettings").Get<JwtSettings>() ?? throw new ArgumentException("JwtSettings is not set in configuration");
        services.Configure<JwtSettings>(config.GetSection("JwtSettings"));


        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(
            opt =>
            {
                opt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSetting.Issuer,
                    ValidAudience = jwtSetting.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSetting.SecretKey)),
                    ClockSkew = TimeSpan.Zero
                };
                opt.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        // Check cookies if no Authorization header
                        if (string.IsNullOrEmpty(context.Token))
                        {
                            var cookieService = context.HttpContext.RequestServices.GetRequiredService<ICookieService>();
                            var tokens = cookieService.GetAuthTokensFromCookies(context.HttpContext);
                            context.Token = tokens?.AccessToken;
                        }
                        return Task.CompletedTask;
                    }
                };

            }
        );

        // Services
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<ICookieService, CookieService>();
        
        



        return services;
    }
}

using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Dapper;
using Infrastructure.Auth;
using Infrastructure.Auth.Types;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Threading.RateLimiting;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString, IConfiguration config)
    {


        #region Services
        services.AddScoped<CurrentUserService, CurrentUserService>();
        services.AddScoped<CookieService, CookieService>();
        services.AddScoped<TokenService, TokenService>();
        services.AddScoped<RealtimeService, RealtimeService>();
        services.AddTransient<ExternalAuthService, ExternalAuthService>();
        services.AddSingleton<PasswordService, PasswordService>();
        services.AddHttpContextAccessor();
        #endregion

        services.AddSignalR();

        var perfSettings = config.GetSection("PerformanceSettings").Get<Application.Data.PerformanceSettings>() 
                           ?? new Application.Data.PerformanceSettings();

        // 2. Rate Limiting: Prevent API abuse (200 requests / minute per user)
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                var userId = context.User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value ?? 
                             context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
                
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: userId,
                    factory: partition => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = perfSettings.RateLimitMaxRequestsPerMinute, 
                        Window = TimeSpan.FromMinutes(1)
                    });
            });
        });

        #region Authentication
        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
        var jwtSettings = config.GetSection("JwtSettings").Get<JwtSettings>() ?? throw new ArgumentException("JwtSettings is not set in configuration");

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
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
                    ClockSkew = TimeSpan.Zero
                };

                opt.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        if (string.IsNullOrEmpty(context.Token))
                        {
                            var cookieService = context.HttpContext.RequestServices.GetRequiredService<CookieService>();
                            var tokens = cookieService.GetAuthTokensFromCookies();
                            context.Token = tokens?.AccessToken;
                        }
                        return Task.CompletedTask;
                    }
                };
            });
        #endregion

        return services;
    }
}



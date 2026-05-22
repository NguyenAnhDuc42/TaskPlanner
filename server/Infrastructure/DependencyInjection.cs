using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Application.Interfaces;
using Application.Interfaces.Data;
using Dapper;
using Infrastructure.Auth;
using Infrastructure.Auth.Types;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Infrastructure.Configuration;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString, IConfiguration config)
    {
        #region Database & Data Access
        DefaultTypeMap.MatchNamesWithUnderscores = true;

        var perfSettings = config.GetSection("PerformanceSettings").Get<PerformanceSettings>() 
                           ?? new PerformanceSettings();

        services.Configure<PerformanceSettings>(config.GetSection("PerformanceSettings"));

        if (!services.Any(d => d.ServiceType == typeof(DbContextOptions<TaskPlanDbContext>) || d.ServiceType == typeof(TaskPlanDbContext)))
        {
            services.AddDbContextPool<TaskPlanDbContext>(options => 
            {
                options.UseNpgsql(connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: perfSettings.DatabaseMaxRetryCount, 
                        maxRetryDelay: TimeSpan.FromSeconds(perfSettings.DatabaseMaxRetryDelaySeconds), 
                        errorCodesToAdd: null);
                });
            }, poolSize: perfSettings.DbContextPoolSize);
        }

        services.AddScoped<IDbConnection>(_ => new Npgsql.NpgsqlConnection(connectionString));
        services.AddScoped<IDataBase, Database>();
        services.AddHostedService<DbContextPreWarmer>();
        #endregion

        #region Services
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<ICookieService, CookieService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IRealtimeService, SignalRRealtimeService>();
        services.AddTransient<IExternalAuthService, ExternalAuthService>();
        services.AddSingleton<IPasswordService, PasswordService>();
        services.AddHttpContextAccessor();
        #endregion

        #region Cache & SignalR
        services.AddSignalR();
        // 1. L1 Cache: Strict RAM limit to prevent OOM kills in low-resource environments
        services.AddMemoryCache(options => 
        {
            options.SizeLimit = 1024 * 1024 * perfSettings.MemoryCacheLimitMB; 
        });
        services.AddHybridCache(options =>
        {
            options.MaximumPayloadBytes = 1024 * 1024; // 1MB
            options.MaximumKeyLength = 512;
            options.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(5),
                LocalCacheExpiration = TimeSpan.FromMinutes(5)
            };
        });

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
        #endregion

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
                            var cookieService = context.HttpContext.RequestServices.GetRequiredService<ICookieService>();
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

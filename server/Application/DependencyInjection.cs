using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration config)
    {
        #region Handlers Registration
        services.Scan(scan => scan.FromAssemblyOf<ApplicationAssemblyMarker>()
            .AddClasses(classes => classes.AssignableTo(typeof(ICommandHandler<>)), publicOnly: false)
                .AsImplementedInterfaces()
                .WithScopedLifetime()
            .AddClasses(classes => classes.AssignableTo(typeof(ICommandHandler<,>)), publicOnly: false)
                .AsImplementedInterfaces()
                .WithScopedLifetime()
            .AddClasses(classes => classes.AssignableTo(typeof(IQueryHandler<,>)), publicOnly: false)
                .AsImplementedInterfaces()
                .WithScopedLifetime()
        );
        
        services.AddScoped<IHandler, HandlerDispatcher>();
        
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        #endregion

        #region Pipelines
        services.Decorate(typeof(ICommandHandler<>), typeof(PipelineDecorator.CommandBaseHandler<>));
        services.Decorate(typeof(ICommandHandler<,>), typeof(PipelineDecorator.CommandHandler<,>));
        services.Decorate(typeof(IQueryHandler<,>), typeof(PipelineDecorator.QueryHandler<,>));
        #endregion

        #region Services & Contexts
        services.AddScoped<WorkspaceContext>();
        services.AddScoped<WorkspaceService>();
        #endregion

        #region Database & Caching
        var perfSettings = config.GetSection("PerformanceSettings").Get<PerformanceSettings>() 
                           ?? new PerformanceSettings();

        var cacheSettings = config.GetSection(CacheSettings.SectionName).Get<CacheSettings>() 
                           ?? new CacheSettings();
        services.Configure<CacheSettings>(config.GetSection(CacheSettings.SectionName));

        // 1. L1 Cache: Strict RAM limit to prevent OOM kills in low-resource environments
        services.AddMemoryCache(options => 
        {
            options.SizeLimit = 1024 * 1024 * cacheSettings.MemoryCacheLimitMB; 
        });
        
        #pragma warning disable EXTEXP0018
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
        #pragma warning restore EXTEXP0018

        var connectionString = config.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

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
        #endregion

        #region Services
        services.AddScoped<CurrentUserService>();
        services.AddScoped<CookieService>();
        services.AddScoped<TokenService>();
        services.AddScoped<RealtimeService>();
        services.AddTransient<ExternalAuthService>();
        services.AddSingleton<PasswordService>();
        services.AddHttpContextAccessor();
        
        // Cursor Helper Configuration
        services.Configure<CursorEncryptionOptions>(config.GetSection(CursorEncryptionOptions.SectionName));
        services.AddScoped<CursorHelper>();
        services.AddScoped<PermissionService>();
        #endregion

        services.AddSignalR();

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

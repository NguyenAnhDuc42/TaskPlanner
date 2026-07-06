using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Security.Claims;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using System.Threading.RateLimiting;
using System.Reflection;
namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration config)
    {
        ValidateRequiredSecrets(config);

        #region Handlers Registration
        // Scan both Application (legacy vertical-slice handlers) and the entry
        // assembly (Api — new minimal-API sync slices) so handlers placed in
        // either project get registered the same way.
        var entryAssembly = Assembly.GetEntryAssembly();
        var handlerAssemblies = entryAssembly is null
            ? [typeof(ApplicationAssemblyMarker).Assembly]
            : new[] { typeof(ApplicationAssemblyMarker).Assembly, entryAssembly };

        services.Scan(scan => scan.FromAssemblies(handlerAssemblies)
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
        
        foreach (var assembly in handlerAssemblies)
        {
            services.AddValidatorsFromAssembly(assembly);
        }
        #endregion

        #region Pipelines
        services.Decorate(typeof(ICommandHandler<>), typeof(PipelineDecorator.CommandBaseHandler<>));
        services.Decorate(typeof(ICommandHandler<,>), typeof(PipelineDecorator.CommandHandler<,>));
        services.Decorate(typeof(IQueryHandler<,>), typeof(PipelineDecorator.QueryHandler<,>));
        #endregion

        #region Services & Contexts
        services.AddScoped<WorkspaceContext>();
        services.AddScoped<WorkspaceService>();
        services.AddScoped<WorkspaceMembershipResolver>();
        #endregion

        #region Database & Caching
        var perfSettings = config.GetSection(PerformanceSettings.SectionName).Get<PerformanceSettings>()
                           ?? new PerformanceSettings();

        var cacheSettings = config.GetSection(CacheSettings.SectionName).Get<CacheSettings>() 
                           ?? new CacheSettings();
        services.Configure<CacheSettings>(config.GetSection(CacheSettings.SectionName));

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
        services.AddScoped<SyncQueryService>();
        services.AddHttpContextAccessor();

        services.Configure<CursorEncryptionOptions>(config.GetSection(CursorEncryptionOptions.SectionName));
        services.AddScoped<CursorHelper>();
        services.AddScoped<PermissionService>();
        services.AddScoped<NotificationService>();
        services.AddHostedService<CleanupService>();
        services.AddScoped<IdempotencyService>();
        #endregion

        #region Email
        services.Configure<EmailSettings>(config.GetSection(EmailSettings.SectionName));
        services.AddTransient<SmtpEmailService>();
        #endregion

        services.AddSignalR();

        #region Authentication
        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
        var jwtSettings = config.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? throw new ArgumentException("JwtSettings is not set in configuration");
        var appSettings = config.GetSection(AppSettings.SectionName).Get<AppSettings>() ?? new AppSettings();
        var oauthSettings = config.GetSection(OAuthSettings.SectionName).Get<OAuthSettings>() ?? new OAuthSettings();

        services.Configure<AppSettings>(config.GetSection(AppSettings.SectionName));
        services.Configure<JwtSettings>(config.GetSection(JwtSettings.SectionName));
        services.Configure<CookieSettings>(config.GetSection(CookieSettings.SectionName));
        services.Configure<OAuthSettings>(config.GetSection(OAuthSettings.SectionName));

        var authBuilder = services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(opt =>
            {
                opt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = appSettings.BackendUrl,
                    ValidAudience = appSettings.FrontendUrl,
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
            })
            .AddCookie("External", opt =>
            {
                opt.Cookie.SameSite = SameSiteMode.Lax;
                opt.Cookie.HttpOnly = true;
                opt.ExpireTimeSpan = TimeSpan.FromMinutes(15);
            });

        if (!string.IsNullOrEmpty(oauthSettings.Google.ClientId))
        {
            authBuilder.AddOAuth("Google", opt =>
            {
                opt.SignInScheme = "External";
                opt.ClientId = oauthSettings.Google.ClientId;
                opt.ClientSecret = oauthSettings.Google.ClientSecret;
                opt.CallbackPath = "/api/auth/callback/google";
                opt.Events.OnRedirectToAuthorizationEndpoint = ctx =>
                {
                    var uri = new UriBuilder(ctx.RedirectUri) { Scheme = "https", Port = -1 };
                    ctx.Response.Redirect(uri.ToString());
                    return Task.CompletedTask;
                };
                opt.AuthorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
                opt.TokenEndpoint = "https://oauth2.googleapis.com/token";
                opt.UserInformationEndpoint = "https://www.googleapis.com/oauth2/v3/userinfo";
                opt.Scope.Add("openid");
                opt.Scope.Add("email");
                opt.Scope.Add("profile");
                opt.CorrelationCookie.SameSite = SameSiteMode.Lax;
                opt.CorrelationCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                opt.Events = new OAuthEvents
                {
                    OnRemoteFailure = ctx =>
                    {
                        var logger = ctx.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("OAuth");
                        logger.LogError(ctx.Failure, "OAuth remote failure: {Message}", ctx.Failure?.Message);
                        ctx.HandleResponse();
                        ctx.Response.Redirect($"{appSettings.FrontendUrl}/auth/sign-in?error=oauth_failed");
                        return Task.CompletedTask;
                    },
                    OnCreatingTicket = async ctx =>
                    {
                        var req = new HttpRequestMessage(HttpMethod.Get, ctx.Options.UserInformationEndpoint);
                        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ctx.AccessToken);
                        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        var res = await ctx.Backchannel.SendAsync(req, ctx.HttpContext.RequestAborted);
                        res.EnsureSuccessStatusCode();
                        var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync(ctx.HttpContext.RequestAborted));
                        var root = doc.RootElement;
                        if (root.TryGetProperty("sub", out var sub))
                            ctx.Identity!.AddClaim(new Claim(ClaimTypes.NameIdentifier, sub.GetString()!));
                        if (root.TryGetProperty("email", out var email))
                            ctx.Identity!.AddClaim(new Claim(ClaimTypes.Email, email.GetString()!));
                        if (root.TryGetProperty("name", out var name))
                            ctx.Identity!.AddClaim(new Claim(ClaimTypes.Name, name.GetString()!));
                    }
                };
            });
        }

        if (!string.IsNullOrEmpty(oauthSettings.GitHub.ClientId))
        {
            authBuilder.AddOAuth("GitHub", opt =>
            {
                opt.SignInScheme = "External";
                opt.ClientId = oauthSettings.GitHub.ClientId;
                opt.ClientSecret = oauthSettings.GitHub.ClientSecret;
                opt.CallbackPath = "/api/auth/callback/github";
                opt.AuthorizationEndpoint = "https://github.com/login/oauth/authorize";
                opt.TokenEndpoint = "https://github.com/login/oauth/access_token";
                opt.UserInformationEndpoint = "https://api.github.com/user";
                opt.Scope.Add("user:email");
                opt.CorrelationCookie.SameSite = SameSiteMode.Lax;
                opt.CorrelationCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                opt.Events = new OAuthEvents
                {
                    OnRedirectToAuthorizationEndpoint = ctx =>
                    {
                        var uri = new UriBuilder(ctx.RedirectUri) { Scheme = "https", Port = -1 };
                        ctx.Response.Redirect(uri.ToString());
                        return Task.CompletedTask;
                    },
                    OnRemoteFailure = ctx =>
                    {
                        var logger = ctx.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("OAuth");
                        logger.LogError(ctx.Failure, "OAuth remote failure: {Message}", ctx.Failure?.Message);
                        ctx.HandleResponse();
                        ctx.Response.Redirect($"{appSettings.FrontendUrl}/auth/sign-in?error=oauth_failed");
                        return Task.CompletedTask;
                    },
                    OnCreatingTicket = async ctx =>
                    {
                        var req = new HttpRequestMessage(HttpMethod.Get, ctx.Options.UserInformationEndpoint);
                        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ctx.AccessToken);
                        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        req.Headers.Add("User-Agent", "TaskPlanner");
                        var res = await ctx.Backchannel.SendAsync(req, ctx.HttpContext.RequestAborted);
                        res.EnsureSuccessStatusCode();
                        var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync(ctx.HttpContext.RequestAborted));
                        var root = doc.RootElement;
                        if (root.TryGetProperty("id", out var id))
                            ctx.Identity!.AddClaim(new Claim(ClaimTypes.NameIdentifier, id.GetRawText()));
                        var displayName = root.TryGetProperty("name", out var ghName) && ghName.ValueKind != JsonValueKind.Null
                            ? ghName.GetString()
                            : null;
                        var login = root.TryGetProperty("login", out var ghLogin) ? ghLogin.GetString() : null;
                        ctx.Identity!.AddClaim(new Claim(ClaimTypes.Name, displayName ?? login ?? ""));
                        if (root.TryGetProperty("email", out var ghEmail) && ghEmail.ValueKind != JsonValueKind.Null)
                            ctx.Identity!.AddClaim(new Claim(ClaimTypes.Email, ghEmail.GetString()!));

                        if (!ctx.Identity!.HasClaim(c => c.Type == ClaimTypes.Email))
                        {
                            var emailReq = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user/emails");
                            emailReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ctx.AccessToken);
                            emailReq.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                            emailReq.Headers.Add("User-Agent", "TaskPlanner");
                            var emailRes = await ctx.Backchannel.SendAsync(emailReq, ctx.HttpContext.RequestAborted);
                            if (emailRes.IsSuccessStatusCode)
                            {
                                var emails = JsonDocument.Parse(await emailRes.Content.ReadAsStringAsync(ctx.HttpContext.RequestAborted));
                                var primary = emails.RootElement.EnumerateArray()
                                    .FirstOrDefault(e =>
                                        e.GetProperty("primary").GetBoolean() &&
                                        e.GetProperty("verified").GetBoolean());
                                if (primary.ValueKind != JsonValueKind.Undefined)
                                    ctx.Identity.AddClaim(new Claim(ClaimTypes.Email, primary.GetProperty("email").GetString()!));
                            }
                        }
                    }
                };
            });
        }
        #endregion

        return services;
    }

    private static void ValidateRequiredSecrets(IConfiguration config)
    {
        var errors = new List<string>();

        var jwtKey = config[$"{JwtSettings.SectionName}:SecretKey"];
        if (string.IsNullOrWhiteSpace(jwtKey))
            errors.Add($"  • {JwtSettings.SectionName}:SecretKey — JWT signing key is missing.\n    Run: dotnet user-secrets set \"{JwtSettings.SectionName}:SecretKey\" \"<32+ char random string>\"");
        else if (jwtKey.Length < 32)
            errors.Add($"  • {JwtSettings.SectionName}:SecretKey — too short ({jwtKey.Length} chars, minimum 32).");

        var connStr = config.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connStr))
            errors.Add($"  • ConnectionStrings:DefaultConnection — database connection string is missing.\n    Run: dotnet user-secrets set \"ConnectionStrings:DefaultConnection\" \"<postgres connection string>\"");

        var cursorKey = config[$"{CursorEncryptionOptions.SectionName}:Key"];
        if (string.IsNullOrWhiteSpace(cursorKey))
            errors.Add($"  • {CursorEncryptionOptions.SectionName}:Key — cursor encryption key is missing.\n    Run: dotnet user-secrets set \"{CursorEncryptionOptions.SectionName}:Key\" \"<32+ char random string>\"");

        if (errors.Count > 0)
            throw new InvalidOperationException(
                $"\n\nMissing required configuration. Set the following via user-secrets (dev) or environment variables (prod):\n\n{string.Join("\n\n", errors)}\n");
    }
}

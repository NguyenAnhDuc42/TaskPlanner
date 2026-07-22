using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);


builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Host.UseSerilog((context, loggerConfig) => 
{
    loggerConfig.ReadFrom.Configuration(context.Configuration)
                .Enrich.FromLogContext()
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [User: {UserId}] {Message:lj}{NewLine}{Exception}"
                );
});

// --- 1. Aspire Defaults ---
builder.AddServiceDefaults();
builder.Configuration.ValidateRequiredSecrets(); 
builder.AddRedisClient("Redis");
builder.Services.AddOpenApi();
builder.Services.AddHttpContextAccessor();

const string CorsPolicy = "AllowFrontend";

var appSettings = builder.Configuration.GetSection(AppSettings.SectionName).Get<AppSettings>() ?? new AppSettings();

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicy, policy =>
    {
        policy.WithOrigins(appSettings.FrontendUrl)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
    options.AddPolicy("AllowSignalR", policy =>
    {
        policy.WithOrigins(appSettings.FrontendUrl)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// --- 2. Infrastructure & Data ---

builder.Services.AddApplication(builder.Configuration);
builder.Services.AddScoped<SyncPermissionService>();
builder.EnrichNpgsqlDbContext<TaskPlanDbContext>();

var rateLimitSettings = builder.Configuration.GetSection(RateLimitSettings.SectionName).Get<RateLimitSettings>() ?? new RateLimitSettings();
builder.Services.Configure<RateLimitSettings>(builder.Configuration.GetSection(RateLimitSettings.SectionName));

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var userId = context.User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value ?? 
                     context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
        
        return RateLimitPartition.GetTokenBucketLimiter(
            partitionKey: userId,
            factory: partition => new TokenBucketRateLimiterOptions
            {
                TokenLimit = rateLimitSettings.MaxRequestsPerMinute,
                QueueLimit = 0,
                ReplenishmentPeriod = TimeSpan.FromSeconds(10),
                TokensPerPeriod = Math.Max(1, rateLimitSettings.MaxRequestsPerMinute / 6),
                AutoReplenishment = true
            });
    });
});



var app = builder.Build();

// --- Auth Configuration Summary ---
{
    var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();

    if (startupLogger.IsEnabled(LogLevel.Information))
    {
        var cfg = builder.Configuration;

        var jwt     = cfg.GetSection(JwtSettings.SectionName).Get<JwtSettings>()       ?? new JwtSettings();
        var app_    = cfg.GetSection(AppSettings.SectionName).Get<AppSettings>()        ?? new AppSettings();
        var cookies = cfg.GetSection(CookieSettings.SectionName).Get<CookieSettings>()  ?? new CookieSettings();
        var email   = cfg.GetSection(EmailSettings.SectionName).Get<EmailSettings>()    ?? new EmailSettings();
        var oauth   = cfg.GetSection(OAuthSettings.SectionName).Get<OAuthSettings>()    ?? new OAuthSettings();

        var jwtSecret    = string.IsNullOrWhiteSpace(jwt.SecretKey)          ? "⚠ NOT SET" : $"SET ({jwt.SecretKey.Length} chars)";
        var emailStatus  = string.IsNullOrWhiteSpace(email.Host)             ? "⚠ NOT CONFIGURED (forgot-password disabled)" : $"{email.Host}:{email.Port}";
        var googleStatus = string.IsNullOrWhiteSpace(oauth.Google.ClientId)  ? "DISABLED" : "ENABLED";
        var githubStatus = string.IsNullOrWhiteSpace(oauth.GitHub.ClientId)  ? "DISABLED" : "ENABLED";

        startupLogger.LogInformation("""

        ══════════════════════════════════════════════════
          AUTH CONFIGURATION
        ══════════════════════════════════════════════════
          URLs
            Frontend  :  {FrontendUrl}
            Backend   :  {BackendUrl}
          JWT
            Secret    :  {JwtSecret}
            Access    :  {AccessMin} min
            Refresh   :  {RefreshDays} days
          Cookies
            Domain    :  {CookieDomain}
            Secure    :  {Secure}
            SameSite  :  {SameSite}
          Email (SMTP)
            Host      :  {EmailHost}
            From      :  {FromAddress} ({FromName})
          OAuth
            Google    :  {Google}
            GitHub    :  {GitHub}
        ══════════════════════════════════════════════════
        """,
            app_.FrontendUrl, app_.BackendUrl,
            jwtSecret, jwt.Expiration, jwt.RefreshExpiration,
            cookies.Domain, cookies.UseSecure, cookies.SameSite,
            emailStatus, email.FromAddress, email.FromName,
            googleStatus, githubStatus);
    }
}

// --- 3. Aspire Endpoints & Monitoring ---
app.MapDefaultEndpoints(); // Standardizes /health and /alive across all containers

if (app.Environment.IsDevelopment())
{
    // Scalar API Documentation
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("TaskPlanner API")
               .WithTheme(ScalarTheme.BluePlanet);
    });
}



// --- Middleware Pipeline ---
app.UseForwardedHeaders();
app.UseRouting();
app.UseCors(CorsPolicy);

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.UseMiddleware<LogContextMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Request Start Logger for visual clarity
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("\n\n>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>\n[REQUEST START] {Method} {Path}", context.Request.Method, context.Request.Path);
    await next(context);
});

app.UseSerilogRequestLogging(options => {
    options.MessageTemplate = "[REQUEST END] HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms\n<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<\n\n";
});

app.UseMiddleware<WorkspaceContextMiddleware>();
app.UseMiddleware<IdempotencyMiddleware>();


app.MapHub<WorkspaceHub>("/hubs/workspace").RequireCors("AllowSignalR");
app.MapHub<SyncHub>("/hubs/sync").RequireCors("AllowSignalR");
app.MapAllEndpoints(typeof(Program).Assembly);

// TEMPORARY — isolated Redis connectivity check, no SignalR involved. Delete once the backplane
// reconnection issue is confirmed fixed. Not auth-gated (doesn't leak the password, just
// connectivity status against a private-network-only host) so it's testable via a raw browser hit.
app.MapGet("/diagnostics/redis-check", async (IConfiguration config) =>
{
    var connStr = config.GetConnectionString("Redis");
    if (string.IsNullOrWhiteSpace(connStr))
        return Results.Text("ConnectionStrings:Redis is not set.", statusCode: 500);

    try
    {
        var options = StackExchange.Redis.ConfigurationOptions.Parse(connStr);
        options.AbortOnConnectFail = false;
        options.ConnectTimeout = 5000;
        await using var mux = await StackExchange.Redis.ConnectionMultiplexer.ConnectAsync(options);
        var endpoints = mux.GetEndPoints().Select(e => e.ToString());
        var pong = await mux.GetDatabase().PingAsync();
        return Results.Text($"Connected: {mux.IsConnected}\nResolved endpoints: {string.Join(", ", endpoints)}\nPing: {pong.TotalMilliseconds}ms");
    }
    catch (Exception ex)
    {
        return Results.Text($"FAILED: {ex.GetType().FullName}: {ex.Message}\n\n{ex}", statusCode: 500);
    }
});

// Apply pending EF Core migrations on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TaskPlanDbContext>();
    await db.Database.MigrateAsync();
}

await app.RunAsync();



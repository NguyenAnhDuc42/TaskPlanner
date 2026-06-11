using System.Security.Claims;
using Npgsql;
using System.Text.Json.Serialization;
using Application;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Scalar.AspNetCore;
using Api;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddOpenApi();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });
builder.Services.AddHttpContextAccessor();

const string CorsPolicy = "AllowFrontend";

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicy, policy =>
    {
        policy.WithOrigins("https://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// --- 2. Infrastructure & Data ---

builder.Services.AddApplication(builder.Configuration);
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
app.UseRouting();
app.UseCors(CorsPolicy);

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

// Now that we have Auth, push the User ID into the logging context
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
app.UseRateLimiter();

app.MapHub<WorkspaceHub>("/hubs/workspace");
app.MapControllers();

await app.RunAsync();



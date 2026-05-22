using System.Security.Claims;
using Npgsql;
using System.Text.Json.Serialization;
using Application;
using Background;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

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
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.EnrichNpgsqlDbContext<Application.Data.TaskPlanDbContext>();
builder.Services.AddApplication(builder.Configuration);

var rateLimitSettings = builder.Configuration.GetSection(Application.Configuration.RateLimitSettings.SectionName).Get<Application.Configuration.RateLimitSettings>() ?? new Application.Configuration.RateLimitSettings();
builder.Services.Configure<Application.Configuration.RateLimitSettings>(builder.Configuration.GetSection(Application.Configuration.RateLimitSettings.SectionName));

builder.Services.AddRateLimiter(options =>
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
                PermitLimit = rateLimitSettings.MaxRequestsPerMinute, 
                Window = TimeSpan.FromMinutes(1)
            });
    });
});


/* 

*/
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



app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<WorkspaceContextMiddleware>();
app.UseRateLimiter();

// --- Middleware ---
if (app.Environment.IsDevelopment())
{
    // Scalar documentation is now auto-mapped in MapDefaultEndpoints() from ServiceDefaults
}

app.UseRouting();
app.UseCors(CorsPolicy);

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<IdempotencyMiddleware>();

// app.MapGet("/", () => Results.Ok(new { status = "ok", time = DateTime.UtcNow }));
app.MapHub<WorkspaceHub>("/hubs/workspace");
app.MapControllers();

app.Run();



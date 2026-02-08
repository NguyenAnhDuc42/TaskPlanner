using System.Security.Claims;
using Api.Middlewares;
using Application.Dependencies;
using Background.Dependencies;
using Infrastructure;
using Infrastructure.Hubs;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// --- 1. Aspire Defaults ---
builder.AddServiceDefaults();
builder.Services.AddOpenApi();
builder.Services.AddControllers();
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
// Aspire automatically injects the connection string from the AppHost
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// Use the Npgsql Aspire component
builder.AddNpgsqlDbContext<Infrastructure.Data.TaskPlanDbContext>("DefaultConnection");
builder.Services.AddInfrastructure(connectionString, builder.Configuration);
builder.Services.AddApplication(builder.Configuration);

// NOTE: In the API, we only add the Background CLIENTS. 
// We do NOT call AddHangfireServer() here (that's the Worker's job).
builder.Services.AddBackground(builder.Configuration);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var scheduler = scope.ServiceProvider
        .GetRequiredService<HangfireJobScheduler>();

    scheduler.Schedule();
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



app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<WorkspaceContextMiddleware>();

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

app.MapGet("/", () => Results.Ok(new { status = "ok", time = DateTime.UtcNow }));
app.MapHub<WorkspaceHub>("/hubs/workspace");
app.MapControllers();

app.Run();

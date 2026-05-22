using System.Security.Claims;
using Npgsql;
using System.Text.Json.Serialization;
using Api.Middlewares;
using Application;
using Background;
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
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddInfrastructure(connectionString, builder.Configuration);
builder.EnrichNpgsqlDbContext<TaskPlanDbContext>();
builder.Services.AddApplication(builder.Configuration);


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

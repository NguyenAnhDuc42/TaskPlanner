using Infrastructure;
using Infrastructure.Data;
using Application.Dependencies;
using Background.Dependencies;
using Hangfire;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// --- 1. Aspire Defaults ---
builder.AddServiceDefaults();

// --- 2. Infrastructure ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// Use the Npgsql Aspire component to enable monitoring
builder.AddNpgsqlDbContext<TaskPlanDbContext>("DefaultConnection");

// Register our shared libraries
builder.Services.AddInfrastructure(connectionString, builder.Configuration);
builder.Services.AddApplication(builder.Configuration);
builder.Services.AddBackground(builder.Configuration);

// --- 3. Hangfire Worker (Execution) ---
// We call AddHangfireServer() here because this is the project that will actually RUN the jobs.
builder.Services.AddHangfireServer();

var app = builder.Build();

// --- 4. Middleware & Endpoints ---
app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    // Host the dashboard so we can see it from the Aspire Dashboard
    app.UseHangfireDashboard("/hangfire");
    
    // Scalar API Documentation
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("TaskPlanner Worker")
               .WithTheme(Scalar.AspNetCore.ScalarTheme.Mars);
    });
}

app.MapGet("/", () => "TaskPlanner Background Worker is running.");

app.Run();

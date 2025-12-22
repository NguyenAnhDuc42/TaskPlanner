using System.Security.Claims;
using Api.Middlewares;
using Application.Dependencies;
using Background.Dependencies;
using Domain;
using Infrastructure;
using Infrastructure.Hubs;
using Microsoft.OpenApi;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<WorkspaceContext>();
const string CorsPolicy = "AllowFrontend";

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicy, policy =>
    {
        policy.WithOrigins("http://localhost:3000", "https://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// --- Swagger / OpenAPI ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "TaskPlanner API", Version = "v1" });
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddInfrastructure(connectionString, builder.Configuration);
builder.Services.AddApplication(builder.Configuration);
builder.Services.AddBackground(builder.Configuration);
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

app.Use(async (context, next) =>
{
    var workspaceIdHeader = context.Request.Headers["X-Workspace-Id"].FirstOrDefault();

    if (string.IsNullOrEmpty(workspaceIdHeader))
    {
        workspaceIdHeader = context.Request.Query["workspaceId"].ToString();
    }

    if (Guid.TryParse(workspaceIdHeader, out var workspaceId))
    {
        var wsContext = context.RequestServices.GetRequiredService<WorkspaceContext>();
        wsContext.WorkspaceId = workspaceId;
    }

    await next();
});

app.UseMiddleware<ExceptionHandlingMiddleware>();
// --- Middleware ---
if (app.Environment.IsDevelopment())
{
    // app.UseDeveloperExceptionPage();

    // Swagger must be before routing/auth
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "TaskPlanner API V1");
        c.RoutePrefix = "swagger"; // URL: /swagger/index.html
    });
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

using System.Security.Claims;
using Background.Dependencies;
using Domain;
using Infrastructure.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddBackground(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapHub<WorkspaceHub>("/hubs/workspace");

app.UseHttpsRedirection();

app.UseAuthorization();
app.Use(async (context, next) =>
{
    var workspaceIdHeader = context.Request.Headers["X-Workspace-Id"].FirstOrDefault();
    var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    if (Guid.TryParse(workspaceIdHeader, out var workspaceId))
    {
        var wsContext = context.RequestServices.GetRequiredService<WorkspaceContext>();
        wsContext.WorkspaceId = workspaceId;
    }

    await next();
});

app.MapControllers();

app.Run();


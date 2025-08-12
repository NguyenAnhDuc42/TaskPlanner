using System.Text.Json.Serialization;
using src.Helper.Extensions;
using Microsoft.OpenApi.Models;
using src.Helper.Middleware; // Add this using statement

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddHttpContextAccessor();
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddPermissionSystem();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "TaskPlanner API", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter a valid token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "Bearer"
    });

    options.AddSecurityDefinition("WorkspaceId", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter the Workspace ID",
        Name = "X-Workspace-Id",
        Type = SecuritySchemeType.ApiKey
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        },
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "WorkspaceId" } },
            Array.Empty<string>()
        }
    });
});

builder.Services.ServiceCollection(builder.Configuration)
                .SupportExtensions(builder.Configuration)
                .IdentityService(builder.Configuration);

builder.Services.AddCors(opt =>
{
    opt.AddPolicy("AllowClient", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:3001").AllowAnyHeader().AllowAnyMethod().AllowCredentials();
    });
});

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.MapGet("/", context =>
{
    context.Response.Redirect("/swagger");
    return Task.CompletedTask;
});

app.UseHttpsRedirection();
app.UseCors("AllowClient");
app.UseMiddleware<GlobalExceptionHandlingMiddleware>(); // Replaced app.UseGlobalExceptionHandler();
app.UseAuthentication();
app.UsePermissionSystem();
app.UseAuthorization();

app.MapControllers();

app.Run();
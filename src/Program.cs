using System.Text.Json.Serialization    ;
using src.Helper.Extensions;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddHttpContextAccessor();
builder.Services.AddControllers().AddJsonOptions(options => // <-- ADD THIS TO USE NEWTONSOFT.JSON
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddPermissionSystem();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "TaskPlanner API", Version = "v1" });

    // Define the Bearer token security scheme for JWT
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter a valid token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "Bearer"
    });

    // Define the Workspace ID header
    options.AddSecurityDefinition("WorkspaceId", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter the Workspace ID",
        Name = "X-Workspace-Id",
        Type = SecuritySchemeType.ApiKey
    });

    // Make sure Swagger UI requires these headers
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
        policy.WithOrigins("http://localhost:3000","http://localhost:3001").AllowAnyHeader().AllowAnyMethod().AllowCredentials();
    });
});

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    // Use Swagger to generate the OpenAPI spec
    app.UseSwagger();
    // Use the Swagger UI to provide an interactive API documentation page
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowClient");
app.UseGlobalExceptionHandler();
app.UseAuthentication();
app.UsePermissionSystem(); // This adds the WorkspaceClaimsMiddleware to the pipeline
app.UseAuthorization();


app.MapGet("/", context =>
{
    context.Response.Redirect("/swagger");
    return Task.CompletedTask;
});

app.MapControllers();

app.Run();

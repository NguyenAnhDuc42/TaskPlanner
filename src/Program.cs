using src.Helper.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddHttpContextAccessor();
builder.Services.AddControllers();

builder.Services.AddSwaggerGen();

builder.Services.ServiceCollection(builder.Configuration)
                .SupportExtensions(builder.Configuration)
                .IdentityService(builder.Configuration);

builder.Services.AddCors(opt =>
{
    opt.AddPolicy("AllowClient", policy =>
    {
        policy.WithOrigins("http://localhost:3000").AllowAnyHeader().AllowAnyMethod().AllowCredentials();
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
app.UseAuthentication();
app.UseAuthorization();


app.MapGet("/", context =>
{
    context.Response.Redirect("/swagger");
    return Task.CompletedTask;
});

app.MapControllers();

app.Run();

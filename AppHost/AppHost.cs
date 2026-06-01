using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// --- 1. Register Resources ---

// PostgreSQL (Local Instance)
// We use AddConnectionString so Aspire doesn't spin up a container.
// It will look for "ConnectionStrings:DefaultConnection" in appsettings.json or User Secrets.
var postgres = builder.AddConnectionString("DefaultConnection");

// Redis (Still container, as it's lightweight and easy)
var redis = builder.AddRedis("cache");



// --- 2. Register Services ---

// The Background Worker
// (Worker removed)

// The Main API
builder.AddProject<Projects.Api>("api")
    .WithReference(postgres)
    .WithReference(redis);

// var viteApp = builder.AddViteApp("frontend", "../frontend")
//     .WithReference(api);


await builder.Build().RunAsync();

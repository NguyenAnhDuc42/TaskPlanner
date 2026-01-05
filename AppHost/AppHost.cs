using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// --- 1. Register Resources ---

// PostgreSQL (Local Instance)
// We use AddConnectionString so Aspire doesn't spin up a container.
// It will look for "ConnectionStrings:DefaultConnection" in appsettings.json or User Secrets.
var postgres = builder.AddConnectionString("DefaultConnection");

// Redis (Still container, as it's lightweight and easy)
var redis = builder.AddRedis("cache");

// MailDev (Container)
// var maildev = builder.AddContainer("maildev", "maildev/maildev")
//     .WithHttpEndpoint(port: 1080, targetPort: 1080, name: "http")
//     .WithEndpoint(port: 1025, targetPort: 1025, name: "smtp");

// --- 2. Register Services ---

// The Background Worker
var worker = builder.AddProject<Projects.Worker>("worker")
    .WithReference(postgres) // Injects "ConnectionStrings:postgres"
    .WithReference(redis);

// The Main API
var api = builder.AddProject<Projects.Api>("api")
    .WithReference(postgres)
    .WithReference(redis);
// .WithReference(maildev);

var viteApp = builder.AddViteApp("frontend", "../frontend")
    .WithReference(api);


builder.Build().Run();

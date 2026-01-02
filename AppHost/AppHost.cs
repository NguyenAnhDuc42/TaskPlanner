var builder = DistributedApplication.CreateBuilder(args);

// --- 1. Register Resources (Containers) ---

// PostgreSQL
var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin(); // Optional: UI for DB management
var db = postgres.AddDatabase("DefaultConnection");

// Redis (For Cache & SignalR)
var redis = builder.AddRedis("cache");

// MailDev (Fake SMTP for local testing)
//var maildev = builder.AddContainer("maildev", "maildev/maildev")
//    .WithHttpEndpoint(port: 1080, targetPort: 1080, name: "http")
//    .WithEndpoint(port: 1025, targetPort: 1025, name: "smtp");

// --- 2. Register Services (Your Code) ---

// The Background Worker
var worker = builder.AddProject<Projects.Worker>("worker")
    .WithReference(db)
    .WithReference(redis);

// The Main API
builder.AddProject<Projects.Api>("api")
    .WithReference(db)
    .WithReference(redis);
builder.Build().Run();

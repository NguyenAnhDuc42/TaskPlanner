using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Background.Services;

public class DatabaseKeepAliveService
{
    private readonly string _connectionString;
    private readonly ILogger<DatabaseKeepAliveService> _logger;

    public DatabaseKeepAliveService(IConfiguration configuration, ILogger<DatabaseKeepAliveService> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        _logger = logger;
    }

    public async Task KeepAlive()
    {
        try
        {
            // Use a raw Npgsql connection to avoid circular dependency on Infrastructure/DbContext
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            
            using var command = new NpgsqlCommand("SELECT 1", connection);
            await command.ExecuteScalarAsync();
            
            _logger.LogInformation("Database keep-alive heartbeat sent at {Time}", DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send database keep-alive heartbeat");
        }
    }
}

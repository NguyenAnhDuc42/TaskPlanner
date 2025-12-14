using System;
using System.Threading.Tasks;
using Application.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class ExternalAuthService : IExternalAuthService
{
    private readonly ILogger<ExternalAuthService> _logger;

    public ExternalAuthService(ILogger<ExternalAuthService> logger)
    {
        _logger = logger;
    }

    public Task<ExternalUser> ValidateAsync(string provider, string token)
    {
        // STUB IMPLEMENTATION
        // In a real app, this would use Google.Apis.Auth or Octokit (GitHub) to validate the token.
        
        _logger.LogInformation("Validating external token for provider: {Provider}", provider);

        if (string.IsNullOrWhiteSpace(token) || token == "invalid")
        {
            throw new Exception("Invalid external token.");
        }

        // Simulate successful validation based on Provider
        string externalId;
        string email;
        string name;

        switch (provider.ToLower())
        {
            case "google":
                externalId = $"google_{token.GetHashCode()}";
                email = $"user.google.{token.GetHashCode()}@gmail.com";
                name = "Google User";
                break;
            case "github":
                externalId = $"github_{token.GetHashCode()}";
                email = $"dev.github.{token.GetHashCode()}@users.noreply.github.com";
                name = "GitHub Developer";
                break;
            default:
                // Generic fallback for other providers
                externalId = $"ext_{provider}_{token.GetHashCode()}";
                email = $"user_{provider}_{token.GetHashCode()}@test.com";
                name = $"User {provider}";
                break;
        }

        _logger.LogInformation("Stub: Validated {Provider} user {Email}", provider, email);

        return Task.FromResult(new ExternalUser(provider, externalId, email, name));
    }
}

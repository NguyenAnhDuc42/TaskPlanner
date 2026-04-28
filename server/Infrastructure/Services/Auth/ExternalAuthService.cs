using Application.Interfaces;

namespace Infrastructure.Auth;

public class ExternalAuthService : IExternalAuthService
{
    public Task<ExternalUserDto> ValidateAsync(string provider, string token)
    {
        // TODO: Implement actual OAuth validation logic for Google/GitHub etc.
        // For now, we return a mock or placeholder to allow the project to build.
        throw new NotImplementedException("External Authentication validation is not yet implemented.");
    }
}

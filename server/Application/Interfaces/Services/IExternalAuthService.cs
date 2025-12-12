using System.Threading.Tasks;

namespace Application.Interfaces.Services;

public record ExternalUser(string Provider, string ExternalId, string Email, string Name);

public interface IExternalAuthService
{
    Task<ExternalUser> ValidateAsync(string provider, string token);
}

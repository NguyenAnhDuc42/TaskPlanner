namespace Application.Interfaces;

public interface IExternalAuthService
{
    Task<ExternalUserDto> ValidateAsync(string provider, string token);
}

public record ExternalUserDto(
    string Email,
    string Name,
    string Provider,
    string ExternalId
);

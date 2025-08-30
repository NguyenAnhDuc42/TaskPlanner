namespace Infrastructure.Auth;

public record class JwtSettings
(
    string SecretKey,
    string Issuer,
    string Audience,
    int Expiration = 15,
    int RefreshExpiration = 30
);

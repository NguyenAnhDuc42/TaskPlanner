namespace Infrastructure.Auth;

public class JwtSettings
{
    public string SecretKey { get; set; } = default!;
    public string Issuer { get; set; } = default!;
    public string Audience { get; set; } = default!;
    public int Expiration { get; set; } = 15;
    public int RefreshExpiration { get; set; } = 30;
}
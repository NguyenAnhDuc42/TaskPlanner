namespace Api;

public class JwtSettings
{
    public const string SectionName = "JwtSettings";

    public string SecretKey { get; set; } = default!;
    public int Expiration { get; set; } = 15;
    public int RefreshExpiration { get; set; } = 30;
}


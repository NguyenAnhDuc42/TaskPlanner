using Microsoft.AspNetCore.Http;

namespace Infrastructure.Auth.Types;

public class CookieSettings
{
    public bool UseSecure { get; set; } = true;
    public string AccessTokenCookieName { get; set; } = "acct";
    public string RefreshTokenCookieName { get; set; } = "rft";
    public string Domain { get; set; } = string.Empty;
    public SameSiteMode SameSite { get; set; } = SameSiteMode.Strict;
}

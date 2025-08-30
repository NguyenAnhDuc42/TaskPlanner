using Microsoft.AspNetCore.Http;

namespace Infrastructure.Auth.Types;

public record class CookieSettings
(
    bool UseSecure = true,
    string AccessTokenCookieName = "acct",
    string RefreshTokenCookieName = "rft",
    string Domain = "",
    SameSiteMode SameSite = SameSiteMode.Strict
);

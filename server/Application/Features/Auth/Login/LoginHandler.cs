using System;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Application;

public class LoginHandler(
    TaskPlanDbContext db, 
    PasswordService passwordService, 
    TokenService tokenService, 
    CookieService cookieService, 
    IHttpContextAccessor httpContextAccessor,
    IMemoryCache cache
) : ICommandHandler<LoginCommand, LoginResponse>
{
    public async Task<Result<LoginResponse>> Handle(LoginCommand request, CancellationToken ct)
    {
        var httpContext = httpContextAccessor.HttpContext;
        var ipAddress = httpContext?.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        var lockoutKey = $"Lockout_{request.email}_{ipAddress}";

        // 1. Check Lockout State
        if (cache.TryGetValue(lockoutKey, out int failedAttempts) && failedAttempts >= 5)
        {
            return Result<LoginResponse>.Failure(Error.Validation("Auth.Locked", "Too many failed login attempts. Please try again in 15 minutes."));
        }

        var user = await db.Users.ByEmail(request.email).AsNoTracking().FirstOrDefaultAsync(ct);
        
        // 2. Validate Credentials & Track Attempts
        if (user is null || !passwordService.VerifyPassword(request.password, user.PasswordHash!)) 
        {
            var attempts = cache.GetOrCreate(lockoutKey, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15);
                entry.Size = 1;
                return 0;
            });
            attempts++;
            cache.Set(lockoutKey, attempts, new MemoryCacheEntryOptions 
            { 
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15),
                Size = 1 
            });

            return Result<LoginResponse>.Failure(AuthError.InvalidCredentials);
        }
        
        // 3. Clear Lockout State on Success
        cache.Remove(lockoutKey);
        
        var userAgent = httpContext?.Request.Headers["User-Agent"].ToString() ?? "Unknown";
        
        var tokens = tokenService.GenerateTokens(user, userAgent, ipAddress);

        var session = Session.Create(user.Id, tokens.RefreshToken, tokens.ExpirationRefreshToken, userAgent, ipAddress);
        await db.Sessions.AddAsync(session, ct);
        await db.SaveChangesAsync(ct);

        cookieService.SetAuthCookies(tokens);

        return Result<LoginResponse>.Success(new LoginResponse(
            tokens.ExpirationAccessToken, 
            tokens.ExpirationRefreshToken
        ));
    }
}

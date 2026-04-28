using Application.Common.Errors;
using Application.Common.Results;
using Application.Interfaces.Data;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Auth;

public class LoginHandler(
    IDataBase db, 
    IPasswordService passwordService, 
    ITokenService tokenService, 
    ICookieService cookieService, 
    IHttpContextAccessor httpContextAccessor
) : ICommandHandler<LoginCommand, LoginResponse>
{
    public async Task<Result<LoginResponse>> Handle(LoginCommand request, CancellationToken ct)
    {
        var user = await db.Users.ByEmail(request.email).AsNoTracking().FirstOrDefaultAsync(ct);
        
        if (user is null || !passwordService.VerifyPassword(request.password, user.PasswordHash!)) 
            return Result<LoginResponse>.Failure(AuthError.InvalidCredentials);
        
        var httpContext = httpContextAccessor.HttpContext;
        var userAgent = httpContext?.Request.Headers["User-Agent"].ToString() ?? "Unknown";
        var ipAddress = httpContext?.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        
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

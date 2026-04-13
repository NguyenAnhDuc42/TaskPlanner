using Application.Common.Errors;
using Application.Common.Results;
using Application.Interfaces.Data;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Auth.Login;

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
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is null) 
            return Result<LoginResponse>.Failure(Error.Failure("Auth.ContextError", "Unable to get HttpContext."));

        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == request.email, ct);
        
        if (user is null || !passwordService.VerifyPassword(request.password, user.PasswordHash!)) 
            return Result<LoginResponse>.Failure(AuthError.InvalidCredentials);
        
        var userAgent = httpContext.Request.Headers["User-Agent"].ToString();
        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown IP";
        
        var tokens = tokenService.GenerateTokens(user, userAgent, ipAddress);
        cookieService.SetAuthCookies(httpContext, tokens);

        var session = Session.Create(user.Id, tokens.RefreshToken, tokens.ExpirationRefreshToken, userAgent, ipAddress);
        await db.Sessions.AddAsync(session, ct);
        
        await db.SaveChangesAsync(ct);
        
        return Result<LoginResponse>.Success(new LoginResponse(
            tokens.ExpirationAccessToken, 
            tokens.ExpirationRefreshToken
        ));
    }
}

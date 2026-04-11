using Application.Common.Errors;
using Application.Interfaces.Data;
using Application.Common.Results;
using Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Application.Features.Auth.Login;

public class LoginHandler : ICommandHandler<LoginCommand, LoginResponse>
{
    private readonly IDataBase _db;
    private readonly IPasswordService _passwordService;
    private readonly ITokenService _tokenService;
    private readonly ICookieService _cookieService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public LoginHandler(
        IDataBase db, 
        IPasswordService passwordService, 
        ITokenService tokenService, 
        ICookieService cookieService, 
        IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _passwordService = passwordService;
        _tokenService = tokenService;
        _cookieService = cookieService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Result<LoginResponse>> Handle(LoginCommand request, CancellationToken ct)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null) 
            return Error.Failure("Auth.ContextError", "Unable to get HttpContext.");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.email, ct);
        
        if (user is null || !_passwordService.VerifyPassword(request.password, user.PasswordHash!)) 
            return AuthError.InvalidCredentials;
        
        var userAgent = httpContext.Request.Headers["User-Agent"].ToString();
        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown IP";
        
        var tokens = _tokenService.GenerateTokens(user, userAgent, ipAddress);
        _cookieService.SetAuthCookies(httpContext, tokens);

        var session = Session.Create(user.Id, tokens.RefreshToken, tokens.ExpirationRefreshToken, userAgent, ipAddress);
        await _db.Sessions.AddAsync(session, ct);
        
        await _db.SaveChangesAsync(ct);
        
        return new LoginResponse(tokens.ExpirationAccessToken, tokens.ExpirationRefreshToken);
    }
}

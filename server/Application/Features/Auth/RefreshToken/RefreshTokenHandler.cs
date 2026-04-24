using Application.Interfaces.Data;
using Application.Common.Results;
using Application.Common.Errors;
using Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Application.Interfaces;

namespace Application.Features.Auth.RefreshToken;

public class RefreshTokenHandler : ICommandHandler<RefreshTokenCommand, RefreshTokenResponse>
{
    private readonly IDataBase _db;
    private readonly ICookieService _cookieService;
    private readonly ITokenService _tokenService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public RefreshTokenHandler(
        IDataBase db, 
        ICookieService cookieService, 
        ITokenService tokenService, 
        IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _cookieService = cookieService;
        _tokenService = tokenService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Result<RefreshTokenResponse>> Handle(RefreshTokenCommand request, CancellationToken ct)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null) 
            return Error.Failure("Auth.ContextError", "Unable to get HttpContext from IHttpContextAccessor.");

        var refreshToken = _cookieService.GetRefreshTokenFromCookies(httpContext);
        if (string.IsNullOrEmpty(refreshToken)) 
            return Error.Unauthorized("Auth.MissingToken", "Refresh token missing.");

        var session = await _db.Sessions
            .ByRefreshToken(refreshToken)
            .Include(s => s.User)
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);
        
        if (session is null || !session.IsActive) 
            return Error.Unauthorized("Auth.InvalidSession", "Invalid or expired session.");

        var user = session.User;
        if (user is null) 
            return Error.Unauthorized("Auth.UserNotFound", "User associated with session not found.");

        var tokens = _tokenService.RefreshAccessToken(session, user);

        var refreshDuration = _tokenService.GetRefreshTokenDuration();
        session.ExtendExpiration(refreshDuration);
        
        await _db.SaveChangesAsync(ct);
        
        _cookieService.SetAuthCookies(httpContext, tokens);

        return new RefreshTokenResponse(tokens.ExpirationAccessToken, tokens.ExpirationRefreshToken);
    }
}

using Application.Common.Results;
using Application.Common.Errors;
using Application.Interfaces.Data;
using Application.Interfaces.Services;
using Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Application.Interfaces;

namespace Application.Features.Auth;

public class ExternalLoginHandler : ICommandHandler<ExternalLoginCommand, LoginResponse>
{
    private readonly IDataBase _db;
    private readonly IExternalAuthService _externalAuthService;
    private readonly ITokenService _tokenService;
    private readonly ICookieService _cookieService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ExternalLoginHandler(
        IDataBase db, 
        IExternalAuthService externalAuthService,
        ITokenService tokenService,
        ICookieService cookieService,
        IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _externalAuthService = externalAuthService;
        _tokenService = tokenService;
        _cookieService = cookieService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Result<LoginResponse>> Handle(ExternalLoginCommand request, CancellationToken ct)
    {
        // 1. Validate Token with Provider
        var externalUser = await _externalAuthService.ValidateAsync(request.Provider, request.Token);

        // 2. Find or Create User
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == externalUser.Email, ct);
        
        if (user == null)
        {
            // JIT Provisioning
            user = User.CreateExternal(externalUser.Name, externalUser.Email, externalUser.Provider, externalUser.ExternalId);
            await _db.Users.AddAsync(user, ct);
            await _db.SaveChangesAsync(ct);
        }
        else if (!user.IsLinkedToProvider(request.Provider))
        {
            // Existing user, link OAuth account
            user.LinkExternalAccount(request.Provider, externalUser.ExternalId);
            await _db.SaveChangesAsync(ct);
        }

        // 3. Generate Tokens and Create Session
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null) 
            return Error.Failure("Auth.ContextError", "No HttpContext available for external login.");

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

using Application.Features.Auth.Login;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Application.Features.Auth.OAuth;

public class ExternalLoginHandler : IRequestHandler<ExternalLoginCommand, LoginResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IExternalAuthService _externalAuthService;
    private readonly ITokenService _tokenService;
    private readonly ICookieService _cookieService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ExternalLoginHandler(
        IUnitOfWork unitOfWork, 
        IExternalAuthService externalAuthService,
        ITokenService tokenService,
        ICookieService cookieService,
        IHttpContextAccessor httpContextAccessor)
    {
        _unitOfWork = unitOfWork;
        _externalAuthService = externalAuthService;
        _tokenService = tokenService;
        _cookieService = cookieService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<LoginResponse> Handle(ExternalLoginCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate Token with Provider
        var externalUser = await _externalAuthService.ValidateAsync(request.Provider, request.Token);

        // 2. Find or Create User
        var user = await _unitOfWork.Set<User>().FirstOrDefaultAsync(u => u.Email == externalUser.Email, cancellationToken);
        
        if (user == null)
        {
            // JIT Provisioning
            user = User.CreateExternal(externalUser.Name, externalUser.Email, externalUser.Provider, externalUser.ExternalId);
            await _unitOfWork.Set<User>().AddAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken); 
        }

        // 3. Generate Tokens (Login)
        var httpContext = _httpContextAccessor.HttpContext ?? throw new Exception("No HttpContext");
        var userAgent = httpContext.Request.Headers["User-Agent"].ToString();
        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown IP";

        var tokens = await _tokenService.GenerateTokensAsync(user, userAgent, ipAddress, cancellationToken);
        _cookieService.SetAuthCookies(httpContext, tokens);

        user.Login(tokens.RefreshToken, tokens.ExpirationRefreshToken, userAgent, ipAddress);
        _unitOfWork.Set<User>().Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new LoginResponse(tokens.ExpirationAccessToken, tokens.ExpirationRefreshToken);
    }
}

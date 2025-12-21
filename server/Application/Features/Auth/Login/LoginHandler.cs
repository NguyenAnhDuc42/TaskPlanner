using Application.Interfaces.Repositories;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Application.Features.Auth.Login;

public class LoginHandler : IRequestHandler<LoginCommand, LoginResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordService _passwordService;
    private readonly ITokenService _tokenService;
    private readonly ICookieService _cookieService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public LoginHandler(IUnitOfWork unitOfWork, IPasswordService passwordService, ITokenService tokenService, ICookieService cookieService, IHttpContextAccessor httpContextAccessor)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        _cookieService = cookieService ?? throw new ArgumentNullException(nameof(cookieService));
        _passwordService = passwordService ?? throw new ArgumentNullException(nameof(passwordService));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null) throw new Exception("Unable to get HttpContext from IHttpContextAccessor.");

        var user = await _unitOfWork.Set<User>().FirstOrDefaultAsync(u => u.Email == request.email, cancellationToken) 
            ?? throw new UnauthorizedAccessException("Wrong credentials");
        
        if (!_passwordService.VerifyPassword(request.password, user.PasswordHash!)) 
            throw new UnauthorizedAccessException("Wrong credentials");
        
        var userAgent = httpContext.Request.Headers["User-Agent"].ToString();
        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown IP";
        var tokens = _tokenService.GenerateTokens(user, userAgent, ipAddress);
        _cookieService.SetAuthCookies(httpContext, tokens);

        // Create session directly without domain events
        var session = Session.Create(user.Id, tokens.RefreshToken, tokens.ExpirationRefreshToken, userAgent, ipAddress);
        _unitOfWork.Set<Session>().Add(session);
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return new LoginResponse(tokens.ExpirationAccessToken, tokens.ExpirationRefreshToken);
    }
}

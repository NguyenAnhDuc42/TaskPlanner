
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
        if (httpContext is null)
        {
            throw new Exception("Unable to get HttpContext from IHttpContextAccessor.");
        }
        var user = await _unitOfWork.Set<User>().FirstOrDefaultAsync(u => u.Email == request.email, cancellationToken);
        if (user is null)
        {
            throw new Exception("User not found");
        }
        if (_passwordService.VerifyPassword(request.password, user.PasswordHash) is false)
        {
            throw new UnauthorizedAccessException("Wrong credentials");
        }
        var userAgent = httpContext.Request.Headers["User-Agent"].ToString();
        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown IP";
        var tokens = await _tokenService.GenerateTokensAsync(user, userAgent, ipAddress, cancellationToken);
        _cookieService.SetAuthCookies(httpContext, tokens);
        var rep = new LoginResponse(tokens.ExpirationAccessToken, tokens.ExpirationRefreshToken);
        return rep;
    }
}

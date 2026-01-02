using Application.Interfaces.Repositories;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using server.Application.Interfaces;
using Application.Common.Exceptions;
using Microsoft.AspNetCore.Http;

namespace Application.Features.Auth.Register;

public class RegisterHandler : IRequestHandler<RegisterCommand, RegisterResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordService _passwordService;
    private readonly ITokenService _tokenService;
    private readonly ICookieService _cookieService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<RegisterHandler> _logger;

    public RegisterHandler(
        IUnitOfWork unitOfWork, 
        IPasswordService passwordService, 
        ITokenService tokenService, 
        ICookieService cookieService, 
        IHttpContextAccessor httpContextAccessor, 
        ILogger<RegisterHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _passwordService = passwordService;
        _tokenService = tokenService;
        _cookieService = cookieService;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<RegisterResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Registering new user: {Email}", request.email);
        var exists = await _unitOfWork.Set<User>().AnyAsync(u => u.Email == request.email, cancellationToken);
        if (exists)
        {
            _logger.LogWarning("Registration failed: Email {Email} already exists", request.email);
            throw new DuplicateEmailException(request.email);
        }

        var passwordHash = _passwordService.HashPassword(request.password);
        var user = User.Create(request.username, request.email, passwordHash);
        
        await _unitOfWork.Set<User>().AddAsync(user, cancellationToken);
        // Persist User first to satisfy FK constraint for Session
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Auto-login: Generate tokens and set cookies
        var httpContext = _httpContextAccessor.HttpContext ?? throw new Exception("No HttpContext");
        var userAgent = httpContext.Request.Headers["User-Agent"].ToString();
        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown IP";

        var tokens = _tokenService.GenerateTokens(user, userAgent, ipAddress);
        _cookieService.SetAuthCookies(httpContext, tokens);

        var session = Session.Create(user.Id, tokens.RefreshToken, tokens.ExpirationRefreshToken, userAgent, ipAddress);
        _unitOfWork.Set<Session>().Add(session);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User registered and logged in: {UserId}, {Email}", user.Id, user.Email);

        return new RegisterResponse(user.Id, user.Name, user.Email);
    }
}
